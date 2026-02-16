using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class ApproveBusinessHandler
{
    public static async Task<BusinessApproved> Handle(ApproveBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        if (!business.Status.CanTransitionTo(BusinessStatus.Active))
            throw new InvalidOperationException(
                $"İşletme '{business.Status.Slug}' durumundan 'Aktif' durumuna geçirilemez.");

        business.Status = BusinessStatus.Active;
        business.ApprovedAt = DateTime.UtcNow;

        session.Store(business);

        return new BusinessApproved(business.Id, command.ApprovedBy, business.ApprovedAt.Value);
    }
}
