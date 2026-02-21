using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class DeactivateBusinessHandler
{
    public static async Task<BusinessDeactivated> Handle(DeactivateBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        if (!business.Status.CanTransitionTo(BusinessStatus.Inactive))
            throw new DomainException(BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Inactive.Slug));

        business.Status = BusinessStatus.Inactive;

        session.Store(business);

        return new BusinessDeactivated(business.Id, command.Reason);
    }
}
