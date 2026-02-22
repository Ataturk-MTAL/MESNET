using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class MonthlyActivityReportHandler
{
    public static async Task<Guid> Handle(
        CreateMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId, ct);
        if (period is null) throw new DomainException(CoordinationErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(CoordinationErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        var report = new MonthlyActivityReport
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            TeacherId = command.TeacherId,
            Year = command.Year,
            Month = command.Month,
            Activities = command.Activities,
            InstructorComment = command.InstructorComment,
            TeacherComment = command.TeacherComment,
            Status = ReportStatus.Draft
        };

        session.Store(report);
        await session.SaveChangesAsync(ct);

        return report.Id;
    }

    public static async Task Handle(
        UpdateMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(command.ReportId, ct);
        if (report is null)
            throw new DomainException(CoordinationErrors.ReportNotFound(command.ReportId));

        if (report.Status != ReportStatus.Draft)
            throw new DomainException(CoordinationErrors.ReportNotDraft(command.ReportId));

        report.Activities = command.Activities;
        report.InstructorComment = command.InstructorComment;
        report.TeacherComment = command.TeacherComment;

        session.Store(report);
        await session.SaveChangesAsync(ct);
    }

    public static async Task Handle(
        SubmitMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(command.ReportId, ct);
        if (report is null)
            throw new DomainException(CoordinationErrors.ReportNotFound(command.ReportId));

        if (report.Status != ReportStatus.Draft)
            throw new DomainException(CoordinationErrors.ReportNotDraft(command.ReportId));

        report.Status = ReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;

        session.Store(report);
        await session.SaveChangesAsync(ct);
    }

    public static async Task Handle(
        ApproveMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(command.ReportId, ct);
        if (report is null)
            throw new DomainException(CoordinationErrors.ReportNotFound(command.ReportId));

        if (report.Status != ReportStatus.Submitted)
            throw new DomainException(CoordinationErrors.ReportNotSubmitted(command.ReportId));

        report.Status = ReportStatus.Approved;
        report.ApprovedAt = DateTime.UtcNow;

        session.Store(report);
        await session.SaveChangesAsync(ct);
    }
}
