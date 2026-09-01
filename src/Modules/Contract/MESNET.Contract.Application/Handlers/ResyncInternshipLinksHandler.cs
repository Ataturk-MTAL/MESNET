using Marten;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine;

namespace MESNET.Contract.Application.Handlers;

/// <summary>
/// Aktif sözleşmeler için <c>ContractActivated</c>'ı yeniden yayınlar (#248) —
/// <b>tek seferlik geçiş adımı</b>.
///
/// <para><b>Neden gerekti:</b> olay saga'ya hiç ulaşmamıştı (saga kimliği çözülemiyordu) ve
/// ölçüldüğünde <b>2248 saga'nın hiçbirinde</b> <c>contractId</c> yazılı değildi. Yol artık
/// açık ama <b>geçmiş kendiliğinden düzelmez</b>: olay bir daha yayınlanmaz.</para>
///
/// <para><b>Neden Contract modülünde:</b> saga'nın sözleşmeyle bağını kuran veri Contract'ın
/// şemasındadır ve Internship oraya sorgu atamaz. Backfill kaynağın kendisinden, mevcut olay
/// yolu üzerinden yürür — böylece düzeltilen aktarıcı da fiilen sınanmış olur.</para>
///
/// <para><b>Yalnız AKTİF sözleşmeler.</b> <c>Terminated</c>/<c>Completed</c> olayları yeniden
/// yayınlansaydı saga <c>InternshipReplacementRequested</c> ve <c>InternshipCompleted</c>
/// üretirdi — yani <b>yeniden yerleştirme talebi ve staj kapanışı ikinci kez</b> tetiklenirdi.
/// <c>ContractActivated</c> ise yan etkisizdir: saga yalnız <c>ContractId</c> yazar ve
/// <c>Active</c> fazına geçer, aynı olay iki kez gelse sonuç değişmez.</para>
///
/// <para><b>BÜTÜN kiracıları dolaşır — istek kiracısında çalışmaz (#292).</b> Uç
/// <c>platform:tenant:manage</c> ile korunuyor ve o izni taşıyan aktör <b>platform kiracısına</b>
/// düşer; <c>InternshipContract</c> ise kiracı damgalıdır ve orada hiçbir satırı yoktur. Eski
/// sürüm bu yüzden <b>200 döner ve sıfır olay yayınlardı</b> — onarımın yapıldığı sanılırdı.</para>
///
/// <para><b>Olay da kiracıya damgalanır.</b> <c>DeliveryOptions.TenantId</c> verilmeseydi
/// yayınlanan <c>ContractActivated</c> yayınlayanın kiracısını (platform) devralırdı; tüketici
/// saga'yı <b>yanlış kiracıda</b> arayıp bulamaz, hiçbir hata da vermezdi. Kiracı çözümünü
/// yalnız sorgu tarafında düzeltip yayın tarafını unutmak, hatanın yarısını taşımak olurdu.</para>
/// </summary>
public static class ResyncInternshipLinksHandler
{
    public static async Task<ResyncInternshipLinksResult> Handle(
        ResyncInternshipLinks _,
        IDocumentStore store,
        ITenantDirectory tenantDirectory,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantDirectory.GetActiveTenantsAsync(cancellationToken);

        // SmartEnum Marten LINQ'inde kullanılamaz; düz string kopya üzerinden süzülür
        // (bkz. CLAUDE.md — Marten SmartEnum LINQ Kuralları).
        var activeName = ContractStatus.Active.Name;

        var republished = 0;
        var skippedNonActive = 0;

        foreach (var tenant in tenants)
        {
            // Kiracı AÇIKÇA verilir; argümansız session bu depoda yasaktır.
            await using var session = store.QuerySession(tenant);

            var all = await session.Query<InternshipContract>().ToListAsync(cancellationToken);
            var active = all.Where(c => c.StatusName == activeName).ToList();

            foreach (var contract in active)
            {
                await bus.PublishAsync(
                    new ContractActivated(
                        contract.Id, contract.StudentId, contract.BusinessId, DateTime.UtcNow),
                    new DeliveryOptions { TenantId = tenant });
            }

            republished += active.Count;
            skippedNonActive += all.Count - active.Count;
        }

        return new ResyncInternshipLinksResult(republished, skippedNonActive, tenants.Count);
    }
}
