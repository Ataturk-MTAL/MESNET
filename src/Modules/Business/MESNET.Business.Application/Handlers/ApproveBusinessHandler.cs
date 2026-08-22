using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class ApproveBusinessHandler
{
    public static async Task<BusinessApproved> Handle(
        ApproveBusiness command, IDocumentSession session, ICurrentUserService currentUser)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        if (!business.Status.CanTransitionTo(BusinessStatus.Active))
            throw new DomainException(BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Active.Slug));

        business.Status = BusinessStatus.Active;
        business.ApprovedAt = DateTime.UtcNow;

        session.Store(business);

        return new BusinessApproved(business.Id, business.RegisteredByInstitutionId, business.Name, business.Address, business.Location, currentUser.GetFullName(), business.ApprovedAt.Value);
    }
}
