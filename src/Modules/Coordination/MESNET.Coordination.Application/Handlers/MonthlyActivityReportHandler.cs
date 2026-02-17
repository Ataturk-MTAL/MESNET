using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class MonthlyActivityReportHandler
{
    public static async Task<(Result, Guid?)> Handle(
        CreateMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var report = new MonthlyActivityReport
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
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

        return (Result.Success(), report.Id);
    }

    public static async Task<Result> Handle(
        UpdateMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(command.ReportId, ct);
        if (report is null)
            return Result.Failure(CoordinationErrors.ReportNotFound(command.ReportId));

        if (report.Status != ReportStatus.Draft)
            return Result.Failure(CoordinationErrors.ReportNotDraft(command.ReportId));

        report.Activities = command.Activities;
        report.InstructorComment = command.InstructorComment;
        report.TeacherComment = command.TeacherComment;

        session.Store(report);
        await session.SaveChangesAsync(ct);

        return Result.Success();
    }

    public static async Task<Result> Handle(
        SubmitMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(command.ReportId, ct);
        if (report is null)
            return Result.Failure(CoordinationErrors.ReportNotFound(command.ReportId));

        if (report.Status != ReportStatus.Draft)
            return Result.Failure(CoordinationErrors.ReportNotDraft(command.ReportId));

        report.Status = ReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;

        session.Store(report);
        await session.SaveChangesAsync(ct);

        return Result.Success();
    }

    public static async Task<Result> Handle(
        ApproveMonthlyActivityReport command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(command.ReportId, ct);
        if (report is null)
            return Result.Failure(CoordinationErrors.ReportNotFound(command.ReportId));

        if (report.Status != ReportStatus.Submitted)
            return Result.Failure(CoordinationErrors.ReportNotSubmitted(command.ReportId));

        report.Status = ReportStatus.Approved;
        report.ApprovedAt = DateTime.UtcNow;

        session.Store(report);
        await session.SaveChangesAsync(ct);

        return Result.Success();
    }
}
