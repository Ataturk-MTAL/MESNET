using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class GuidanceVisitHandler
{
    public static async Task<Guid> Handle(
        CreateGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId, ct);
        if (period is null) throw new DomainException(CoordinationErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(CoordinationErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        var visit = new GuidanceVisit
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
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

        return visit.Id;
    }

    public static async Task Handle(
        UpdateGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(command.VisitId, ct);
        if (visit is null)
            throw new DomainException(CoordinationErrors.VisitNotFound(command.VisitId));

        if (visit.Status != VisitStatus.Draft)
            throw new DomainException(CoordinationErrors.VisitNotDraft(command.VisitId));

        visit.VisitDate = command.VisitDate;
        visit.StudentNotes = command.StudentNotes;
        visit.InstructorMeetingNotes = command.InstructorMeetingNotes;
        visit.IssuesIdentified = command.IssuesIdentified;
        visit.ActionsTaken = command.ActionsTaken;
        visit.GeneralAssessment = command.GeneralAssessment;

        session.Store(visit);
        await session.SaveChangesAsync(ct);
    }

    public static async Task Handle(
        SubmitGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(command.VisitId, ct);
        if (visit is null)
            throw new DomainException(CoordinationErrors.VisitNotFound(command.VisitId));

        if (visit.Status != VisitStatus.Draft)
            throw new DomainException(CoordinationErrors.VisitNotDraft(command.VisitId));

        visit.Status = VisitStatus.Submitted;
        visit.SubmittedAt = DateTime.UtcNow;

        session.Store(visit);
        await session.SaveChangesAsync(ct);
    }

    public static async Task Handle(
        ApproveGuidanceVisit command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(command.VisitId, ct);
        if (visit is null)
            throw new DomainException(CoordinationErrors.VisitNotFound(command.VisitId));

        if (visit.Status != VisitStatus.Submitted)
            throw new DomainException(CoordinationErrors.VisitNotSubmitted(command.VisitId));

        visit.Status = VisitStatus.Approved;
        visit.ApprovedAt = DateTime.UtcNow;

        session.Store(visit);
        await session.SaveChangesAsync(ct);
    }
}
