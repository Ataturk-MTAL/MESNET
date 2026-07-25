using Marten;
using MESNET.Contract.Shared.Events;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Shared.Events;

namespace MESNET.Internship.Application.Consumers;

// Sınıf adı Handler veya Consumer ile BİTMELİ — Wolverine tip keşfi konvansiyonu bu.
// Eski adı `InternshipSummaryUpdater` idi; hiç keşfedilmiyordu, dolayısıyla buradaki Handle
// metotları hiç çalışmıyordu. Tüketicisi olmayan olay dead letter üretmediği için hata sessizdi.
public static class InternshipSummaryConsumer
{
    public static void Handle(InternshipStarted e, IDocumentSession session)
    {
        var summary = new InternshipSummary
        {
            Id = e.InternshipId,
            PlacementId = e.PlacementId,
            StudentId = e.StudentId,
            StudentName = e.StudentName,
            BusinessId = e.BusinessId,
            BusinessName = e.BusinessName,
            InstitutionId = e.InstitutionId,
            AcademicPeriodId = e.AcademicPeriodId,
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
