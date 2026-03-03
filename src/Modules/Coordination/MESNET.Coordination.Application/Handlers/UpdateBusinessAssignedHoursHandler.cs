using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class UpdateBusinessAssignedHoursHandler
{
    public static async Task Handle(
        UpdateBusinessAssignedHours command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(
            command.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        if (command.AssignedHours <= 0)
            throw new DomainException(CoordinationErrors.InvalidAssignedHours(command.AssignedHours));

        if (command.AssignedHours > view.MaxCoordinationHours)
        {
            throw new DomainException(
                CoordinationErrors.AssignedHoursExceedMax(command.AssignedHours, view.MaxCoordinationHours));
        }

        view.AssignedHours = command.AssignedHours;
        session.Store(view);
    }
}
