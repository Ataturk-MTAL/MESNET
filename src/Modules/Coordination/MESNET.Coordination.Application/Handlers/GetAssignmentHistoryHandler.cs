using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Queries;

namespace MESNET.Coordination.Application.Handlers;

public static class GetAssignmentHistoryHandler
{
    public static async Task<List<AssignmentHistoryEntryDto>> Handle(
        GetAssignmentHistory query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var view = await CoordinationViewLookup.LoadBranchRowAsync(
            session, query.BusinessId, query.BranchCode, query.AcademicPeriodId, cancellationToken);

        if (view is null)
        {
            throw new DomainException(
                CoordinationErrors.BusinessBranchNotFound(query.BusinessId, query.BranchCode));
        }

        // Aktör adı saklanmaz, okuma anında çözülür (#137).
        var names = await UserNameResolver.ResolveAsync(
            session, view.History.Select(h => h.PerformedById), cancellationToken);

        return view.History.Select(h => new AssignmentHistoryEntryDto(
            h.Timestamp,
            h.Action,
            h.PerformedById,
            names.NameOf(h.PerformedById),
            h.TeacherName,
            h.SlotDay,
            h.SlotPeriod,
            h.AssignedHours,
            h.Details
        )).ToList();
    }
}
