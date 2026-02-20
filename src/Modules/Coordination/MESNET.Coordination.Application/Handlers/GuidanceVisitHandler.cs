using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class GuidanceVisitHandler
{
    public static async Task<Result<Guid>> Handle(
        CreateGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var visit = new GuidanceVisit
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            VisitDate = command.VisitDate,
            StudentNotes = command.StudentNotes,
            InstructorMeetingNotes = command.InstructorMeetingNotes,
            IssuesIdentified = command.IssuesIdentified,
            ActionsTaken = command.ActionsTaken,
            GeneralAssessment = command.GeneralAssessment,
            Status = VisitStatus.Draft
        };

        session.Store(visit);
        await session.SaveChangesAsync(ct);

        return Result<Guid>.Success(visit.Id);
    }

    public static async Task<Result> Handle(
        UpdateGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(command.VisitId, ct);
        if (visit is null)
            return Result.Failure(CoordinationErrors.VisitNotFound(command.VisitId));

        if (visit.Status != VisitStatus.Draft)
            return Result.Failure(CoordinationErrors.VisitNotDraft(command.VisitId));

        visit.VisitDate = command.VisitDate;
        visit.StudentNotes = command.StudentNotes;
        visit.InstructorMeetingNotes = command.InstructorMeetingNotes;
        visit.IssuesIdentified = command.IssuesIdentified;
        visit.ActionsTaken = command.ActionsTaken;
        visit.GeneralAssessment = command.GeneralAssessment;

        session.Store(visit);
        await session.SaveChangesAsync(ct);

        return Result.Success();
    }

    public static async Task<Result> Handle(
        SubmitGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(command.VisitId, ct);
        if (visit is null)
            return Result.Failure(CoordinationErrors.VisitNotFound(command.VisitId));

        if (visit.Status != VisitStatus.Draft)
            return Result.Failure(CoordinationErrors.VisitNotDraft(command.VisitId));

        visit.Status = VisitStatus.Submitted;
        visit.SubmittedAt = DateTime.UtcNow;

        session.Store(visit);
        await session.SaveChangesAsync(ct);

        return Result.Success();
    }

    public static async Task<Result> Handle(
        ApproveGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(command.VisitId, ct);
        if (visit is null)
            return Result.Failure(CoordinationErrors.VisitNotFound(command.VisitId));

        if (visit.Status != VisitStatus.Submitted)
            return Result.Failure(CoordinationErrors.VisitNotSubmitted(command.VisitId));

        visit.Status = VisitStatus.Approved;
        visit.ApprovedAt = DateTime.UtcNow;

        session.Store(visit);
        await session.SaveChangesAsync(ct);

        return Result.Success();
    }
}
