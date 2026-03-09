using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class GetAssignmentHistoryHandler
{
    public static async Task<List<AssignmentHistoryEntryDto>> Handle(
        GetAssignmentHistory query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(
            query.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(query.BusinessId));

        return view.History.Select(h => new AssignmentHistoryEntryDto(
            h.Timestamp,
            h.Action,
            h.PerformedBy,
            h.TeacherName,
            h.SlotDay,
            h.SlotPeriod,
            h.AssignedHours,
            h.Details
        )).ToList();
    }
}
