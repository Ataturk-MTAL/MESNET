using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class RejectBusinessHandler
{
    public static async Task<BusinessRejected> Handle(RejectBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        if (!business.Status.CanTransitionTo(BusinessStatus.Rejected))
            throw new DomainException(BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Rejected.Slug));

        business.Status = BusinessStatus.Rejected;

        session.Store(business);

        return new BusinessRejected(business.Id, command.Reason);
    }
}
