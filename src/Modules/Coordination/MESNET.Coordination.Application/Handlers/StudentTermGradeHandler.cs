using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Shared.Events;

namespace MESNET.Coordination.Application.Handlers;

public static class StudentTermGradeHandler
{
    public static async Task<Guid> Handle(
        EnterStudentTermGrade command, IDocumentSession session, CancellationToken ct)
    {
        // 1) Pencere kontrolü — dönem aktif + bugün not giriş penceresinde
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId, ct)
            ?? throw new DomainException(CoordinationErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive)
            throw new DomainException(CoordinationErrors.AcademicPeriodClosed(command.AcademicPeriodId));
        if (!period.IsGradeEntryOpen(DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new DomainException(CoordinationErrors.GradeEntryWindowClosed(command.AcademicPeriodId));

        // 2) Yerleştirme/kapsam kontrolü — öğrenci bu işletmeye yerleştirilmiş olmalı
        var placement = await session.Query<CoordinationPlacedStudentView>()
            .Where(p => p.StudentId == command.StudentId
                        && p.BusinessId == command.BusinessId
                        && p.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainException(CoordinationErrors.StudentNotPlacedAtBusiness(command.StudentId));

        // 3) Upsert — öğrenci + dönem başına tek not; gönderilmişse düzenlenemez
        var existing = await session.Query<StudentTermGrade>()
            .Where(g => g.StudentId == command.StudentId && g.AcademicPeriodId == command.AcademicPeriodId)
            .FirstOrDefaultAsync(ct);

        if (existing is not null && existing.StatusName != StudentTermGradeStatus.Draft.Name)
            throw new DomainException(CoordinationErrors.StudentTermGradeAlreadySubmitted(existing.Id));

        var grade = existing ?? new StudentTermGrade
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            AcademicPeriodId = command.AcademicPeriodId,
        };

        grade.BusinessId = command.BusinessId;
        grade.InstitutionId = placement.InstitutionId;
        grade.TeacherId = placement.TeacherId;
        grade.StudentName = placement.StudentName;
        grade.BranchName = placement.BranchName;
        grade.MasterInstructorName = command.MasterInstructorName;
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

    public static async Task<StudentTermGradeSubmitted> Handle(
        SubmitStudentTermGrade command, IDocumentSession session, CancellationToken ct)
    {
        var grade = await session.LoadAsync<StudentTermGrade>(command.StudentTermGradeId, ct)
            ?? throw new DomainException(CoordinationErrors.StudentTermGradeNotFound(command.StudentTermGradeId));

        // Kapsam — yalnız kendi işletmesinin notunu gönderebilir
        if (grade.BusinessId != command.BusinessId)
            throw new DomainException(CoordinationErrors.StudentNotPlacedAtBusiness(grade.StudentId));

        // Pencere hâlâ açık olmalı
        var period = await session.LoadAsync<AcademicPeriodView>(grade.AcademicPeriodId, ct);
        if (period is null || !period.IsGradeEntryOpen(DateOnly.FromDateTime(DateTime.UtcNow)))
            throw new DomainException(CoordinationErrors.GradeEntryWindowClosed(grade.AcademicPeriodId));

        grade.Status = StudentTermGradeStatus.Submitted;
        grade.StatusName = StudentTermGradeStatus.Submitted.Name;
        grade.SubmittedAt = DateTime.UtcNow;
        session.Store(grade);
        await session.SaveChangesAsync(ct);

        // Cascading event → Reporting StudentTermGradeView'ini besler (fiş gerçek notlardan üretilir)
        return new StudentTermGradeSubmitted(
            grade.Id, grade.StudentId, grade.BusinessId, grade.InstitutionId, grade.AcademicPeriodId,
            grade.PracticeGrades, grade.ServiceGrades, grade.ProjectGrades, grade.ExperimentGrades,
            grade.TermAverage, grade.MasterInstructorName, grade.SubmittedAt.Value);
    }

    // İşletmede verilen 4 kategorinin tüm notlarının aritmetik ortalaması (otomatik dönem ortalaması)
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
