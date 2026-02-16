using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class ActivateBusinessHandler
{
    public static async Task<BusinessActivated> Handle(ActivateBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        if (!business.Status.CanTransitionTo(BusinessStatus.Active))
            throw new InvalidOperationException(
                $"İşletme '{business.Status.Slug}' durumundan 'Aktif' durumuna geçirilemez.");

        business.Status = BusinessStatus.Active;

        session.Store(business);

        return new BusinessActivated(business.Id);
    }
}
