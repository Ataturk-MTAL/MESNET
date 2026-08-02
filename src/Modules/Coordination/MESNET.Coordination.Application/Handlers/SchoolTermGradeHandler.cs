using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

/// <summary>
/// <b>Okulda staj</b> yapan öğrencinin dönem notu (#171).
///
/// <para>İşletme akışından (<see cref="StudentTermGradeHandler"/>) üç noktada ayrılır:</para>
/// <list type="number">
///   <item>Kapsam <c>business_id</c> claim'i değil <b>kurum</b> ve okulda staj yerleştirmesidir —
///         okuldaki şefin <c>business_id</c> claim'i yoktur.</item>
///   <item><c>BusinessId</c> <c>null</c> kalır; kayıt işletme kapsamlı hiçbir sorguya girmez.</item>
///   <item>Gönderim <c>StudentTermGradeSubmitted</c> olayını <b>yayınlamaz</b> — okulda staj için
///         MEB Form 8 üretilmez, Reporting bu kayıttan haberdar olmaz.</item>
/// </list>
/// </summary>
public static class SchoolTermGradeHandler
{
    public static async Task<Guid> Handle(
        EnterSchoolTermGrade command, IDocumentSession session, CancellationToken ct)
    {
        if (command.InstitutionId == Guid.Empty)
            throw new DomainException(CoordinationErrors.SchoolGradeInstitutionScopeMissing());

        // 1) Pencere kontrolü — işletme akışıyla aynı kural
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId, ct)
            ?? throw new DomainException(CoordinationErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive)
            throw new DomainException(CoordinationErrors.AcademicPeriodClosed(command.AcademicPeriodId));
        if (!period.IsGradeEntryOpen(DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new DomainException(CoordinationErrors.GradeEntryWindowClosed(command.AcademicPeriodId));

        // 2) Kapsam — öğrenci gerçekten OKULDA staj yapıyor ve bu kurumda olmalı.
        //    İşletmede staj yapan öğrencinin notu bu uçtan girilemez: girilebilseydi okul,
        //    işletmenin gireceği notu onun yerine yazabilirdi.
        var placement = await session.Query<SchoolPlacedStudentView>()
            .Where(p => p.StudentId == command.StudentId
                        && p.InstitutionId == command.InstitutionId
                        && p.AcademicPeriodId == command.AcademicPeriodId
                        && p.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainException(CoordinationErrors.StudentNotPlacedAtSchool(command.StudentId));

        // 3) Upsert — öğrenci + dönem başına tek not; gönderilmişse düzenlenemez
        var existing = await session.Query<StudentTermGrade>()
            .Where(g => g.StudentId == command.StudentId && g.AcademicPeriodId == command.AcademicPeriodId)
            .FirstOrDefaultAsync(ct);

        if (existing is not null && existing.StatusName != StudentTermGradeStatus.Draft.Name)
            throw new DomainException(CoordinationErrors.StudentTermGradeAlreadySubmitted(existing.Id));

        // Aynı öğrencinin işletme notu varsa bu uçtan ezilemez — akışlar birbirinin üstüne yazamaz.
        if (existing?.BusinessId is not null)
            throw new DomainException(CoordinationErrors.StudentTermGradeBelongsToBusiness(existing.Id));

        var grade = existing ?? new StudentTermGrade
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            AcademicPeriodId = command.AcademicPeriodId,
        };

        grade.BusinessId = null;
        grade.InstitutionId = placement.InstitutionId;
        grade.TeacherId = placement.TeacherId;   // okulda stajda gözetmen
        grade.StudentName = placement.StudentName;
        grade.BranchName = placement.BranchName;
        grade.PracticeGrades = command.PracticeGrades;
        grade.ServiceGrades = command.ServiceGrades;
        grade.ProjectGrades = command.ProjectGrades;
        grade.ExperimentGrades = command.ExperimentGrades;
        grade.TermAverage = ComputeAverage(grade);
        grade.Status = StudentTermGradeStatus.Draft;
        grade.StatusName = StudentTermGradeStatus.Draft.Name;
        grade.EnteredByName = command.EnteredByName;
        grade.UpdatedAt = existing is null ? null : DateTime.UtcNow;

        session.Store(grade);
        await session.SaveChangesAsync(ct);
        return grade.Id;
    }

    /// <summary>
    /// Taslağı kesinleştirir. <b>Olay yayınlamaz</b> — Reporting'in <c>StudentTermGradeView</c>'ı
    /// oluşmaz, dolayısıyla Form 8 üretim yolu bu kayıt için hiç açılmaz.
    /// </summary>
    public static async Task Handle(
        SubmitSchoolTermGrade command, IDocumentSession session, CancellationToken ct)
    {
        var grade = await session.LoadAsync<StudentTermGrade>(command.StudentTermGradeId, ct)
            ?? throw new DomainException(CoordinationErrors.StudentTermGradeNotFound(command.StudentTermGradeId));

        // İşletme notu bu uçtan gönderilemez: gönderilseydi Reporting'e taşıyan olay hiç
        // yayınlanmaz ve o öğrencinin fişi sessizce üretilemez hâle gelirdi.
        if (grade.BusinessId is not null)
            throw new DomainException(CoordinationErrors.StudentTermGradeBelongsToBusiness(grade.Id));

        if (grade.InstitutionId != command.InstitutionId)
            throw new DomainException(CoordinationErrors.StudentNotPlacedAtSchool(grade.StudentId));

        var period = await session.LoadAsync<AcademicPeriodView>(grade.AcademicPeriodId, ct);
        if (period is null || !period.IsGradeEntryOpen(DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new DomainException(CoordinationErrors.GradeEntryWindowClosed(grade.AcademicPeriodId));

        grade.Status = StudentTermGradeStatus.Submitted;
        grade.StatusName = StudentTermGradeStatus.Submitted.Name;
        grade.SubmittedAt = DateTime.UtcNow;
        session.Store(grade);
        await session.SaveChangesAsync(ct);
    }

    // Okulda verilen 4 kategorinin tüm notlarının aritmetik ortalaması
    private static decimal? ComputeAverage(StudentTermGrade g)
    {
        var all = g.PracticeGrades
            .Concat(g.ServiceGrades)
            .Concat(g.ProjectGrades)
            .Concat(g.ExperimentGrades)
            .ToList();
        return all.Count > 0 ? Math.Round((decimal)all.Average(), 2) : null;
    }
}
