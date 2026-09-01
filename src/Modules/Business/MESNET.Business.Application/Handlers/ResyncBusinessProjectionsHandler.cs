using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Shared.Events;
using Wolverine;
using BusinessEntity = MESNET.Business.Core.Entities.Business;

namespace MESNET.Business.Application.Handlers;

public static class ResyncBusinessProjectionsHandler
{
    public static async Task<ResyncBusinessProjectionsResult> Handle(
        ResyncBusinessProjections command, IQuerySession session, IMessageBus bus, CancellationToken ct)
    {
        var businesses = await session.Query<BusinessEntity>().ToListAsync(ct);

        // Alan eşlemesi UpdateBusinessInfoHandler ile birebir aynı — olayın taşıdığı her alan
        // entity'de mevcut, dolayısıyla tüketicilerin denormalize verisi boşalmıyor.
        foreach (var business in businesses)
        {
            await bus.PublishAsync(new BusinessUpdated(
                business.Id,
                business.Name,
                business.Address,
                business.Location,
                business.Sectors,
                business.PhoneNumber,
                business.Email,
                business.MasterInstructor?.FullName,
                business.PersonnelCount,
                business.PrimaryRepresentativeName(),
                // 11. ARGÜMAN ZORUNLU (#295). Varsayılanı false olduğu için geçilmediğinde
                // derleyici susuyordu; tüketici (Payment.BusinessPaymentProfileConsumer) alanı
                // KOŞULSUZ yazdığı için her yayın kamu kurumu bayrağını siliyordu — ve o bayrak
                // 3308 Geçici Md.12 ile devlet katkısı hesabına giriyor.
                business.IsPublicInstitution));
        }

        return new ResyncBusinessProjectionsResult(businesses.Count);
    }
}
