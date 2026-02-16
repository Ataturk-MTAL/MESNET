using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class RejectBusinessHandler
{
    public static async Task<BusinessRejected> Handle(RejectBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        if (!business.Status.CanTransitionTo(BusinessStatus.Rejected))
            throw new InvalidOperationException(
                $"İşletme '{business.Status.Slug}' durumundan 'Reddedildi' durumuna geçirilemez.");

        business.Status = BusinessStatus.Rejected;

        session.Store(business);

        return new BusinessRejected(business.Id, command.Reason);
    }
}
