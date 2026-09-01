using Marten;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Services;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Kopya staj saga'larını tek satıra indirir (#251) — <b>tek seferlik geçiş adımı</b>.
///
/// <para><b>Neden gerekti:</b> <c>Start</c> kimliği <c>Guid.NewGuid()</c> ile üretiyordu ve
/// tekrar yayınlanan <c>StudentPlaced</c> her seferinde yeni saga doğuruyordu. Ölçüldü:
/// 2248 saga, yalnız 95 yerleştirme. Kimlik artık deterministik
/// (<see cref="InternshipSagaId"/>) ama <b>geçmiş kendiliğinden düzelmez</b>.</para>
///
/// <para><b>Hangi kopya korunur:</b> <b>en ileri fazdaki</b>. Fesih sürecine girmiş bir saga'yı
/// atıp <c>AwaitingContract</c>'taki kardeşini tutmak, yürüyen fesih zincirini sessizce
/// iptal etmek olurdu (canlı ölçümde tam bu manzara vardı: 1 saga
/// <c>TerminationInProgress</c>, 23'ü <c>AwaitingContract</c>).</para>
///
/// <para><b>BÜTÜN kiracıları dolaşır — istek kiracısında çalışmaz (#292).</b> Uç
/// <c>platform:tenant:manage</c> ile korunuyor ve o izni taşıyan aktör <b>platform
/// kiracısına</b> düşer; <c>InternshipSaga</c> ise kiracı damgalıdır ve platform kiracısında
/// <b>hiçbir satırı yoktur</b>. Enjekte edilen <c>IDocumentSession</c> ile çalışan eski sürüm
/// bu yüzden <b>200 döner ve sıfır kayıt işlerdi</b>: operatör onarımın yapıldığını sanırdı,
/// fesih zinciri onarılmamış kalırdı. Dev'de görünmemesinin nedeni <c>admin</c> hesabının
/// <c>InstitutionManager</c> ve <c>SystemAdmin</c> rollerini <b>birlikte</b> taşımasıydı.</para>
///
/// <para><b>Neden izin okul düzeyine indirilmedi:</b> bu uç saga kimliğini yeniden yazıp kayıt
/// siler. Okul rollerine vermek, her müdüre yürüyen fesih zincirlerini yeniden şekillendirebilen
/// bir araç vermek olurdu. Eylem gerçekten kurum üstüdür; düzeltilmesi gereken şey izin değil,
/// kiracı çözümüydü.</para>
///
/// <para><b>Kiracı sayısı yanıtta döner</b> (<c>TenantsProcessed</c>): sıfır kiracı, sıfır
/// bulgudan farklı bir şeydir ve ayırt edilebilir olmalıdır.</para>
/// </summary>
public static class ResyncInternshipSagasHandler
{
    public static async Task<ResyncInternshipSagasResult> Handle(
        ResyncInternshipSagas _,
        IDocumentStore store,
        ITenantDirectory tenantDirectory,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantDirectory.GetActiveTenantsAsync(cancellationToken);

        var merged = 0;
        var placements = 0;
        var alreadyCanonical = 0;

        foreach (var tenant in tenants)
        {
            // Kiracı AÇIKÇA verilir. Argümansız session kiracısızdır ve bu depoda yasaktır
            // (TenantlessSessionDriftTests).
            await using var session = store.LightweightSession(tenant);

            var result = await MergeTenantAsync(session, cancellationToken);

            merged += result.Merged;
            placements += result.Placements;
            alreadyCanonical += result.AlreadyCanonical;
        }

        return new ResyncInternshipSagasResult(merged, placements, alreadyCanonical, tenants.Count);
    }

    private static async Task<(int Merged, int Placements, int AlreadyCanonical)> MergeTenantAsync(
        IDocumentSession session, CancellationToken cancellationToken)
    {
        var all = await session.Query<InternshipSaga>().ToListAsync(cancellationToken);

        var merged = 0;
        var alreadyCanonical = 0;
        var groups = all.GroupBy(s => s.PlacementId).ToList();

        foreach (var group in groups)
        {
            var canonicalId = InternshipSagaId.For(group.Key);

            // En ileri faz kazanır. SmartEnum'un Value'su faz sırasını taşıyor
            // (Placed 1 … Completed 6); yürüyen bir süreç geriye alınamaz.
            var winner = group.OrderByDescending(s => s.Phase.Value).First();

            foreach (var loser in group.Where(s => s.Id != winner.Id))
            {
                session.Delete(loser);
                merged++;
            }

            if (winner.Id == canonicalId)
            {
                alreadyCanonical++;
                continue;
            }

            // Kimlik değişiyor: Marten'de birincil anahtar güncellenemez, eski satır silinip
            // yeni kimlikle yazılır. Durumun tamamı taşınır, yalnız kimlik değişir.
            session.Delete(winner);
            session.Store(WithId(winner, canonicalId));
        }

        await session.SaveChangesAsync(cancellationToken);

        return (merged, groups.Count, alreadyCanonical);
    }

    private static InternshipSaga WithId(InternshipSaga source, Guid id) => new()
    {
        Id = id,
        PlacementId = source.PlacementId,
        StudentId = source.StudentId,
        BusinessId = source.BusinessId,
        InstitutionId = source.InstitutionId,
        AcademicPeriodId = source.AcademicPeriodId,
        ContractId = source.ContractId,
        Phase = source.Phase,
        TerminationReason = source.TerminationReason,
        TerminationReasonType = source.TerminationReasonType,
        RequiresParentApproval = source.RequiresParentApproval,
        ApprovalChain = source.ApprovalChain,
        // BU ALAN UNUTULMUŞTU (#295). D2 ile eklendi ve kimlik yeniden yazılırken kopyalanmıyordu:
        // kanonik olmayan kimlikli her saga, onarımdan TerminationRequestedAt = null olarak
        // çıkıyordu. StuckApprovalPolicy null'u "eksik veri sınırı gevşetemez" gerekçesiyle
        // TIKANMIŞ sayar — yani onarım, müdürlük panosunda olmayan tıkanmalar üretirdi.
        TerminationRequestedAt = source.TerminationRequestedAt,
    };
}
