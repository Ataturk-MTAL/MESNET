using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class CloseBusinessHandler
{
    public static async Task<BusinessClosed> Handle(CloseBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        if (!business.Status.CanTransitionTo(BusinessStatus.Closed))
            throw new DomainException(BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Closed.Slug));

        business.Status = BusinessStatus.Closed;
        business.ClosedAt = DateTime.UtcNow;

        session.Store(business);

        return new BusinessClosed(business.Id, business.ClosedAt.Value);
    }
}
