using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class CloseBusinessHandler
{
    public static async Task<BusinessClosed> Handle(CloseBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        if (!business.Status.CanTransitionTo(BusinessStatus.Closed))
            throw new InvalidOperationException(
                $"İşletme '{business.Status.Slug}' durumundan 'Kapatılmış' durumuna geçirilemez.");

        business.Status = BusinessStatus.Closed;
        business.ClosedAt = DateTime.UtcNow;

        session.Store(business);

        return new BusinessClosed(business.Id, business.ClosedAt.Value);
    }
}
