using Marten;
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
/// <para><b>Kiracı başına çalışır:</b> saga kiracıya ait bir belgedir ve bu uç istek
/// bağlamındadır — her okul için ayrı çağrılmalıdır.</para>
/// </summary>
public static class ResyncInternshipSagasHandler
{
    public static async Task<ResyncInternshipSagasResult> Handle(
        ResyncInternshipSagas _, IDocumentSession session)
    {
        var all = await session.Query<InternshipSaga>().ToListAsync();

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

        await session.SaveChangesAsync();

        return new ResyncInternshipSagasResult(merged, groups.Count, alreadyCanonical);
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
    };
}
