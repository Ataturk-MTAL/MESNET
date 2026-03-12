using System.Text.Json;
using Marten;
using MESNET.Common.Infrastructure.Storage;
using MESNET.Common.Shared.Security;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Templates;
using MESNET.Reporting.Core.Entities;
using MESNET.Reporting.Core.Enums;
using MESNET.Reporting.Core.Models;
using MESNET.Reporting.Core.ReadModels;
using QuestPDF.Fluent;

namespace MESNET.Reporting.Application.Handlers;

/// <summary>
/// Form 7: Aylık Devam - Devamsızlık Bildirim Çizelgesi
/// İşletme bazlı — o işletmedeki tüm stajyer öğrencilerin aylık devamsızlığını raporlar.
/// </summary>
public static class GenerateMonthlyAttendanceReportHandler
{
    private const string BucketName = "meb-forms";

    private static readonly string[] TurkishMonths =
        ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
         "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    /// <summary>
    /// Anlık önizleme — PDF byte[] döner, MinIO'ya yazmaz.
    /// </summary>
    public static async Task<byte[]> Handle(GenerateMonthlyAttendancePreview command, IQuerySession session)
    {
        var data = await BuildReportData(session,
            command.InstitutionId, command.AcademicPeriodId,
            command.BusinessId, command.Year, command.Month,
            command.InstitutionName, command.AcademicYear);

        var pdf = new MonthlyAttendanceReportDocument(data);
        return pdf.GeneratePdf();
    }

    /// <summary>
    /// Arşivleme — PDF üretir, MinIO'ya yazar, GeneratedDocument kaydeder.
    /// </summary>
    public static async Task<(Guid, NotifyDocumentGenerated)> Handle(
        GenerateMonthlyAttendanceDocument command, IDocumentSession session, IFileStorageService storage)
    {
        var data = command.Data;

        var doc = new GeneratedDocument
        {
            Id = data.DocumentId,
            FormType = MebFormType.MonthlyAttendanceReport,
            FormDataJson = JsonSerializer.Serialize(data),
            GeneratedByName = command.User.FullName,
            GeneratedByUserId = command.User.UserId,
            GeneratedAt = DateTime.UtcNow,
            BusinessId = data.BusinessId,
            InstitutionId = data.InstitutionId,
            TeacherId = data.TeacherId,
            AcademicYear = data.AcademicYear
        };

        var pdf = new MonthlyAttendanceReportDocument(data);
        var pdfBytes = pdf.GeneratePdf();
        var objectPath = $"{doc.FormType.Name}/{doc.GeneratedAt:yyyy-MM}/{doc.Id}.pdf";

        using var stream = new MemoryStream(pdfBytes);
        var metadata = new Dictionary<string, string>
        {
            ["x-amz-meta-form-type"] = doc.FormType.Name,
            ["x-amz-meta-generated-by"] = doc.GeneratedByName,
            ["x-amz-meta-generated-at"] = doc.GeneratedAt.ToString("O")
        };

        var result = await storage.UploadFileAsync(BucketName, objectPath, stream, "application/pdf", metadata);
        if (result.IsSuccess)
            doc.PdfStoragePath = objectPath;

        session.Store(doc);

        var notification = new NotifyDocumentGenerated(
            doc.Id, doc.FormType.Name, doc.FormType.Slug,
            doc.InstitutionId, doc.TeacherId, doc.StudentId, command.User.FullName);

        return (doc.Id, notification);
    }

    /// <summary>
    /// Read model'lerden rapor verisini birleştirir.
    /// </summary>
    internal static async Task<MonthlyAttendanceReportData> BuildReportData(
        IQuerySession session,
        Guid institutionId, Guid academicPeriodId, Guid businessId,
        int year, int month,
        string institutionName, string academicYear)
    {
        // 1. Bu işletmedeki öğrenci yerleşimleri
        var placements = await session.Query<StudentPlacementReportView>()
            .Where(v => v.InstitutionId == institutionId
                        && v.AcademicPeriodId == academicPeriodId
                        && v.BusinessId == businessId)
            .ToListAsync();

        // 2. İlgili öğrencilerin devamsızlık kayıtları
        var studentIds = placements.Select(p => p.StudentId).Distinct().ToList();
        var attendanceViews = await session.Query<StudentAttendanceReportView>()
            .Where(v => v.Year == year && v.Month == month && studentIds.Contains(v.StudentId))
            .ToListAsync();
        var attendanceMap = attendanceViews.ToDictionary(v => v.StudentId);

        // 3. İş takvimi — tatil günleri
        var calendar = await session.Query<WorkCalendarReportView>()
            .Where(c => c.InstitutionId == institutionId && c.Year == year)
            .FirstOrDefaultAsync();

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var weekendDays = new List<int>();
        var holidayDays = new List<int>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                weekendDays.Add(day);
        }

        if (calendar is not null)
        {
            foreach (var rd in calendar.RestrictedDays)
            {
                var rdDate = DateOnly.FromDateTime(rd.Date);
                if (rdDate.Year == year && rdDate.Month == month)
                    holidayDays.Add(rdDate.Day);
            }
        }

        // İşletme bilgileri — ilk placement'tan al
        var firstPlacement = placements.FirstOrDefault();
        var businessName = firstPlacement?.BusinessName ?? "";
        var businessPhone = firstPlacement?.BusinessPhone;
        var businessEmail = firstPlacement?.BusinessEmail;
        var businessContactName = firstPlacement?.BusinessContactName ?? "";
        var teacherId = firstPlacement?.TeacherId;

        // 4. Öğrenci satırları
        var students = placements.Select(p =>
        {
            attendanceMap.TryGetValue(p.StudentId, out var att);
            return BuildStudentRow(p, att);
        }).ToList();

        return new MonthlyAttendanceReportData
        {
            DocumentId = Guid.NewGuid(),
            InstitutionId = institutionId,
            BusinessId = businessId,
            AcademicPeriodId = academicPeriodId,
            TeacherId = teacherId,
            InstitutionName = institutionName,
            BusinessName = businessName,
            BusinessPhone = businessPhone,
            BusinessEmail = businessEmail,
            AcademicYear = academicYear,
            Year = year,
            Month = month,
            DocumentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            BusinessContactName = businessContactName,
            CoordinatorTeacherName = "", // Frontend'den veya TeacherId lookup ile doldurulabilir
            VicePrincipalName = "",      // Frontend'den veya InstitutionId lookup ile doldurulabilir
            WeekendDays = weekendDays,
            HolidayDays = holidayDays,
            Students = students
        };
    }

    private static StudentAttendanceRow BuildStudentRow(
        StudentPlacementReportView placement, StudentAttendanceReportView? attendance)
    {
        var morningMarks = new Dictionary<int, string?>();
        var afternoonMarks = new Dictionary<int, string?>();
        var excused = 0;
        var unexcused = 0;

        if (attendance is not null)
        {
            foreach (var entry in attendance.AbsentDays)
            {
                var symbol = MapAbsenceTypeToSymbol(entry.AbsenceType);

                // Mevcut sistemde tam gün devamsızlık — hem S hem Ö satırına yaz
                morningMarks[entry.Day] = symbol;
                afternoonMarks[entry.Day] = symbol;

                if (entry.AbsenceType is "Excused" or "HealthReport")
                    excused++;
                else
                    unexcused++;
            }
        }

        return new StudentAttendanceRow
        {
            ClassName = placement.ClassName,
            StudentNumber = placement.StudentNumber,
            StudentName = placement.StudentName,
            BranchName = placement.BranchName,
            MorningMarks = morningMarks,
            AfternoonMarks = afternoonMarks,
            ExcusedAbsences = excused,
            UnexcusedAbsences = unexcused
        };
    }

    /// <summary>
    /// Mevcut 3 AbsenceType → MEB sembol kodu dönüşümü.
    /// İlk fazda: Unexcused → D, Excused → İ, HealthReport → S
    /// </summary>
    private static string MapAbsenceTypeToSymbol(string absenceType) => absenceType switch
    {
        "Unexcused" => "D",
        "Excused" => "İ",
        "HealthReport" => "S",
        _ => "D"
    };
}
