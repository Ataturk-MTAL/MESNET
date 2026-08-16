using Marten;
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
/// <para><b>Kiracı başına çalışır</b> — istek bağlamındaki okulun sözleşmeleri.</para>
/// </summary>
public static class ResyncInternshipLinksHandler
{
    public static async Task<ResyncInternshipLinksResult> Handle(
        ResyncInternshipLinks _, IDocumentSession session, IMessageBus bus)
    {
        // SmartEnum Marten LINQ'inde kullanılamaz; düz string kopya üzerinden süzülür
        // (bkz. CLAUDE.md — Marten SmartEnum LINQ Kuralları).
        var activeName = ContractStatus.Active.Name;

        var all = await session.Query<InternshipContract>().ToListAsync();
        var active = all.Where(c => c.StatusName == activeName).ToList();

        foreach (var contract in active)
        {
            await bus.PublishAsync(new ContractActivated(
                contract.Id, contract.StudentId, contract.BusinessId, DateTime.UtcNow));
        }

        return new ResyncInternshipLinksResult(active.Count, all.Count - active.Count);
    }
}
