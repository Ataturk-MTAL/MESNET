using Marten;
using MESNET.Contract.Shared.Events;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Shared.Events;

namespace MESNET.Internship.Application.Consumers;

public static class InternshipSummaryUpdater
{
    public static void Handle(InternshipStarted e, IDocumentSession session)
    {
        var summary = new InternshipSummary
        {
            Id = e.InternshipId,
            StudentId = e.StudentId,
            BusinessId = e.BusinessId,
            InstitutionId = e.InstitutionId,
            Phase = InternshipPhase.AwaitingContract,
            StartedAt = e.StartedAt,
            LastUpdated = DateTime.UtcNow
        };
        session.Store(summary);
    }

    public static async Task Handle(ContractActivated e, IDocumentSession session)
    {
        var summary = await session.Query<InternshipSummary>()
            .FirstOrDefaultAsync(s => s.StudentId == e.StudentId);
        if (summary is null) return;

        summary.ContractId = e.ContractId;
        summary.Phase = InternshipPhase.Active;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(InternshipCompleted e, IDocumentSession session)
    {
        var summary = await session.LoadAsync<InternshipSummary>(e.InternshipId);
        if (summary is null) return;

        summary.Phase = InternshipPhase.Completed;
        summary.CompletedAt = e.CompletedAt;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(InternshipTerminationApprovalChainStarted e, IDocumentSession session)
    {
        var summary = await session.LoadAsync<InternshipSummary>(e.InternshipId);
        if (summary is null) return;

        summary.Phase = InternshipPhase.TerminationInProgress;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(InternshipReplacementRequested e, IDocumentSession session)
    {
        var summary = await session.Query<InternshipSummary>()
            .FirstOrDefaultAsync(s => s.StudentId == e.StudentId && s.BusinessId == e.OldBusinessId);
        if (summary is null) return;

        summary.Phase = InternshipPhase.Terminated;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }
}
