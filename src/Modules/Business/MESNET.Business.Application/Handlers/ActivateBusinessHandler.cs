using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class ActivateBusinessHandler
{
    public static async Task<BusinessActivated> Handle(ActivateBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        if (!business.Status.CanTransitionTo(BusinessStatus.Active))
            throw new DomainException(BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Active.Slug));

        business.Status = BusinessStatus.Active;

        session.Store(business);

        return new BusinessActivated(business.Id, business.InstitutionId, business.Name, business.Address, business.Location);
    }
}
