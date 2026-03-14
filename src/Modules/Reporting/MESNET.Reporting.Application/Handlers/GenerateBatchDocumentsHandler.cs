using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Errors;
using MESNET.Reporting.Core.Entities;
using MESNET.Reporting.Core.Enums;
using MESNET.Reporting.Core.Models;
using MESNET.Reporting.Core.ReadModels;
using Wolverine;

namespace MESNET.Reporting.Application.Handlers;

/// <summary>
/// Toplu belge oluşturma handler'ı.
/// Müdür/müdür yardımcısı tarafından tetiklenir — aynı dönem için
/// zaten oluşturulmuş belgeler atlanır (duplicate kontrolü).
/// </summary>
public static class GenerateBatchDocumentsHandler
{
    public static async Task<BatchGenerateResult> Handle(
        GenerateBatchDocuments command,
        IQuerySession session,
        IMessageBus bus)
    {
        if (!MebFormType.TryFromName(command.FormType, out var formType))
            throw new DomainException(new Error("INVALID_FORM_TYPE", $"Geçersiz form tipi: {command.FormType}"));

        // Bu ay+formType için zaten oluşturulmuş belgeleri yükle (duplicate kontrolü)
        // FormTypeName string alanı kullanılır — SmartEnum LINQ tuzağından kaçınılır
        var startOfMonth = new DateTime(command.Year, command.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        var existingForType = await session.Query<GeneratedDocument>()
            .Where(d => d.InstitutionId == command.InstitutionId
                        && d.FormTypeName == formType.Name
                        && d.GeneratedAt >= startOfMonth
                        && d.GeneratedAt < endOfMonth)
            .ToListAsync();

        return formType.Name switch
        {
            nameof(MebFormType.MonthlyAttendanceReport) =>
                await GenerateForm7Batch(command, formType, existingForType, session, bus),
            nameof(MebFormType.MonthlyActivityReport) =>
                await GenerateForm2Batch(command, formType, existingForType, session, bus),
            nameof(MebFormType.GuidanceVisit) =>
                await GenerateForm3Batch(command, formType, existingForType, session, bus),
            _ => throw new DomainException(new Error("UNSUPPORTED_BATCH",
                $"'{formType.Slug}' formu için toplu oluşturma desteklenmiyor."))
        };
    }

    /// <summary>
    /// Form 7: Aylık Devamsızlık — işletme bazlı. Her işletme için en fazla 1 belge.
    /// </summary>
    private static async Task<BatchGenerateResult> GenerateForm7Batch(
        GenerateBatchDocuments command,
        MebFormType formType,
        IReadOnlyList<GeneratedDocument> existingDocs,
        IQuerySession session,
        IMessageBus bus)
    {
        var existingBusinessIds = existingDocs
            .Where(d => d.BusinessId.HasValue)
            .Select(d => d.BusinessId!.Value)
            .ToHashSet();

        var placements = await session.Query<StudentPlacementReportView>()
            .Where(p => p.InstitutionId == command.InstitutionId
                        && p.AcademicPeriodId == command.AcademicPeriodId
                        && p.BusinessId != Guid.Empty)
            .ToListAsync();

        if (placements.Count == 0)
            throw new DomainException(new Error("NO_PLACEMENTS",
                "Bu dönem için yerleştirme verisi bulunamadı. Önce öğrenci-işletme eşleştirmesi yapılmalı."));

        var businessGroups = placements
            .GroupBy(p => p.BusinessId)
            .Where(g => !existingBusinessIds.Contains(g.Key))
            .ToList();

        var generated = 0;
        var skipped = existingBusinessIds.Count;

        foreach (var group in businessGroups)
        {
            var data = await GenerateMonthlyAttendanceReportHandler.BuildReportData(
                session,
                command.InstitutionId,
                command.AcademicPeriodId,
                group.Key,
                command.Year, command.Month,
                "",
                command.AcademicYear);

            var generateCommand = new GenerateMonthlyAttendanceDocument(data, command.User);
            await bus.InvokeAsync<Guid>(generateCommand);
            generated++;
        }

        return new BatchGenerateResult(generated, skipped, formType.Slug);
    }

    /// <summary>
    /// Form 2: Aylık Eğitim Faaliyeti — öğrenci bazlı. Her öğrenci-işletme çifti için en fazla 1 belge.
    /// </summary>
    private static async Task<BatchGenerateResult> GenerateForm2Batch(
        GenerateBatchDocuments command,
        MebFormType formType,
        IReadOnlyList<GeneratedDocument> existingDocs,
        IQuerySession session,
        IMessageBus bus)
    {
        var existingStudentBizPairs = existingDocs
            .Where(d => d.StudentId.HasValue && d.BusinessId.HasValue)
            .Select(d => (d.StudentId!.Value, d.BusinessId!.Value))
            .ToHashSet();

        var placements = await session.Query<StudentPlacementReportView>()
            .Where(p => p.InstitutionId == command.InstitutionId
                        && p.AcademicPeriodId == command.AcademicPeriodId
                        && p.BusinessId != Guid.Empty)
            .ToListAsync();

        if (placements.Count == 0)
            throw new DomainException(new Error("NO_PLACEMENTS",
                "Bu dönem için yerleştirme verisi bulunamadı. Önce öğrenci-işletme eşleştirmesi yapılmalı."));

        var generated = 0;
        var skipped = 0;

        foreach (var placement in placements)
        {
            if (existingStudentBizPairs.Contains((placement.StudentId, placement.BusinessId)))
            {
                skipped++;
                continue;
            }

            var formData = new MonthlyActivityFormData
            {
                StudentId = placement.StudentId,
                BusinessId = placement.BusinessId,
                InstitutionId = placement.InstitutionId,
                TeacherId = placement.TeacherId,
                InstitutionName = "",
                StudentFullName = placement.StudentName,
                StudentNumber = placement.StudentNumber,
                BranchName = placement.BranchName,
                BusinessName = placement.BusinessName,
                ClassYear = placement.ClassYear,
                AcademicYear = command.AcademicYear,
                Year = command.Year,
                Month = command.Month,
                Activities = [],
                MasterInstructorName = placement.BusinessContactName ?? "",
                CoordinatorTeacherName = ""
            };

            var generateCommand = new GenerateMonthlyActivityDocument(formData, command.User);
            await bus.InvokeAsync<Guid>(generateCommand);
            generated++;
        }

        return new BatchGenerateResult(generated, skipped, formType.Slug);
    }

    /// <summary>
    /// Form 3: Günlük Rehberlik — öğretmen bazlı.
    /// Her öğretmenin tüm işletme ziyaretleri tek PDF'te toplanır (ikişerli A5 yerleşim).
    /// Duplicate kontrolü: Bu ay için zaten GuidanceVisit dokümanı olan öğretmenler atlanır.
    /// </summary>
    private static async Task<BatchGenerateResult> GenerateForm3Batch(
        GenerateBatchDocuments command,
        MebFormType formType,
        IReadOnlyList<GeneratedDocument> existingDocs,
        IQuerySession session,
        IMessageBus bus)
    {
        // Bu ay için zaten oluşturulmuş öğretmen ID'leri
        var existingTeacherIds = existingDocs
            .Where(d => d.TeacherId.HasValue)
            .Select(d => d.TeacherId!.Value)
            .ToHashSet();

        var placements = await session.Query<StudentPlacementReportView>()
            .Where(p => p.InstitutionId == command.InstitutionId
                        && p.AcademicPeriodId == command.AcademicPeriodId
                        && p.TeacherId != null
                        && p.BusinessId != Guid.Empty)
            .ToListAsync();

        if (placements.Count == 0)
            throw new DomainException(new Error("NO_PLACEMENTS",
                "Bu dönem için yerleştirme verisi bulunamadı. Önce öğrenci-işletme eşleştirmesi yapılmalı."));

        // Öğretmen bazlı grupla
        var teacherGroups = placements
            .Where(p => p.TeacherId.HasValue)
            .GroupBy(p => p.TeacherId!.Value)
            .Where(g => !existingTeacherIds.Contains(g.Key))
            .ToList();

        var generated = 0;
        var skipped = existingTeacherIds.Count;

        // Seçilen ayın iş günlerini hesapla (Pazartesi-Cuma)
        var startOfMonth = new DateOnly(command.Year, command.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var workDays = new List<DateOnly>();
        for (var d = startOfMonth; d <= endOfMonth; d = d.AddDays(1))
        {
            if (d.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
                workDays.Add(d);
        }

        foreach (var teacherGroup in teacherGroups)
        {
            var teacherId = teacherGroup.Key;
            var teacherName = teacherGroup.First().TeacherName;
            if (string.IsNullOrEmpty(teacherName)) teacherName = "—";

            // İşletme bazlı grupla — her işletme-gün çifti için bir form
            var businessGroups = teacherGroup
                .GroupBy(p => p.BusinessId)
                .ToList();

            var forms = new List<GuidanceVisitFormData>();

            foreach (var bizGroup in businessGroups)
            {
                var firstPlacement = bizGroup.First();
                var studentCount = bizGroup.Count();

                // Her iş günü için bir form oluştur
                foreach (var workDay in workDays)
                {
                    forms.Add(new GuidanceVisitFormData
                    {
                        DocumentId = Guid.NewGuid(),
                        BusinessId = firstPlacement.BusinessId,
                        InstitutionId = command.InstitutionId,
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        BusinessName = firstPlacement.BusinessName,
                        BranchName = firstPlacement.BranchName,
                        StudentCount = studentCount,
                        VisitDate = workDay.ToDateTime(TimeOnly.MinValue),
                    });
                }
            }

            if (forms.Count == 0) continue;

            var pageCount = (forms.Count + 1) / 2;
            var batchCommand = new GenerateGuidanceVisitBatchDocument(
                forms, command.User,
                InstitutionId: command.InstitutionId,
                TeacherId: teacherId,
                Description: $"{teacherName} — {forms.Count} ziyaret ({pageCount} sayfa)");

            await bus.InvokeAsync<Guid>(batchCommand);
            generated++;
        }

        return new BatchGenerateResult(generated, skipped, formType.Slug);
    }
}
