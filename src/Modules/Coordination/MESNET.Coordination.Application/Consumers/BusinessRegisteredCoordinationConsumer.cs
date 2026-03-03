using Marten;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme kaydedildiğinde (kurum tarafından, doğrudan Active) coordination view oluşturur.
/// Self-register işletmeler PendingApproval ile başlar — onlar BusinessApproved'da eklenir.
/// </summary>
public static class BusinessRegisteredCoordinationConsumer
{
    public static async Task Consume(
        BusinessRegistered @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // Self-register işletmeler PendingApproval durumunda — haritada gösterilmez
        if (@event.Source == RegistrationSource.SelfRegistered)
            return;

        var existing = await session.LoadAsync<BusinessCoordinationView>(@event.BusinessId, cancellationToken);
        if (existing is not null) return;

        session.Store(new BusinessCoordinationView
        {
            Id = @event.BusinessId,
            Name = @event.Name,
            Address = @event.Address,
            District = AddressHelper.ExtractDistrict(@event.Address),
            Location = @event.Location,
            InstitutionId = @event.TenantId,
            ActiveStudentCount = 0,
        });
    }
}
