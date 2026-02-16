using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class DeactivateBusinessHandler
{
    public static async Task<BusinessDeactivated> Handle(DeactivateBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        if (!business.Status.CanTransitionTo(BusinessStatus.Inactive))
            throw new InvalidOperationException(
                $"İşletme '{business.Status.Slug}' durumundan 'Pasif' durumuna geçirilemez.");

        business.Status = BusinessStatus.Inactive;

        session.Store(business);

        return new BusinessDeactivated(business.Id, command.Reason);
    }
}
