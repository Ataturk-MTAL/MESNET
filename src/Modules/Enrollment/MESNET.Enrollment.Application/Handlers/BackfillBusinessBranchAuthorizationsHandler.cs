using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.Policies;
using MESNET.Enrollment.Shared.Events;
using Wolverine;

namespace MESNET.Enrollment.Application.Handlers;

public static class BackfillBusinessBranchAuthorizationsHandler
{
    public static async Task<BackfillBusinessBranchAuthorizationsResult> Handle(
        BackfillBusinessBranchAuthorizations command,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken ct)
    {
        // SmartEnum LINQ'te karşılaştırılamaz; düz string kopyası StatusName kullanılıyor
        // (bkz. CLAUDE.md — Marten SmartEnum LINQ kuralları).
        var finalStatuses = PlacementStatus.List
            .Where(s => s.IsFinal)
            .Select(s => s.Name)
            .ToArray();

        var placements = await session.Query<InternshipPlacement>()
            .Where(p => !finalStatuses.Contains(p.StatusName))
            .ToListAsync(ct);

        var byBusiness = PlacementBranchPolicy.GroupBranchCodesByBusiness(placements);
        var observedAt = DateTime.UtcNow;
        var branchCount = 0;

        foreach (var (businessId, branchCodes) in byBusiness)
        {
            await bus.PublishAsync(new BusinessBranchUsageObserved(businessId, branchCodes, observedAt));
            branchCount += branchCodes.Count;
        }

        return new BackfillBusinessBranchAuthorizationsResult(byBusiness.Count, branchCount);
    }
}
