using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme kaydedildiğinde (kurum tarafından, doğrudan Active) coordination view oluşturur.
/// Self-register işletmeler PendingApproval ile başlar — onlar BusinessApproved'da eklenir.
/// Lokasyon varsa otomatik mesafe hesaplar (OSRM → Haversine fallback).
/// </summary>
public static class BusinessRegisteredCoordinationConsumer
{
    public static async Task Consume(
        BusinessRegistered @event,
        IDocumentSession session,
        IOsrmDistanceService osrmService,
        CancellationToken cancellationToken)
    {
        // Self-register işletmeler PendingApproval durumunda — haritada gösterilmez
        if (@event.Source == "SelfRegistered") // RegistrationSource.SelfRegistered.Name
            return;

        var existing = await session.LoadAsync<BusinessCoordinationView>(@event.BusinessId, cancellationToken);
        if (existing is not null) return;

        var view = new BusinessCoordinationView
        {
            Id = @event.BusinessId,
            Name = @event.Name,
            Address = @event.Address,
            District = AddressHelper.ExtractDistrict(@event.Address),
            Location = @event.Location,
            InstitutionId = @event.TenantId,
            ActiveStudentCount = 0,
        };

        // Lokasyon varsa otomatik mesafe hesapla
        if (@event.Location is not null)
        {
            await DistanceHelper.CalculateAndSetDistanceAsync(
                view, @event.TenantId, session, osrmService, cancellationToken);
        }

        session.Store(view);
    }
}
