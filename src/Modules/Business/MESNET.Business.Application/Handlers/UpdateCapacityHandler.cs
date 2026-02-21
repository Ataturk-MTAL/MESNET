using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class UpdateCapacityHandler
{
    public static async Task<BusinessCapacityChanged> Handle(UpdateCapacity command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        business.Capacity = new BusinessCapacity
        {
            TotalSlots = command.TotalSlots,
            OccupiedSlots = command.OccupiedSlots
        };

        session.Store(business);

        return new BusinessCapacityChanged(business.Id, business.Capacity.TotalSlots, business.Capacity.OccupiedSlots);
    }
}
