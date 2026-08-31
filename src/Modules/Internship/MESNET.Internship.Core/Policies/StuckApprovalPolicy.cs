using MESNET.Internship.Core.ValueObjects;

namespace MESNET.Internship.Core.Policies;

/// <summary>
/// Bir fesih onay zinciri "tıkanmış" mı (D2).
///
/// <para><b>Faz alanına BAKILMAZ.</b> <c>InternshipSaga.Phase</c> bir SmartEnum'dur ve Marten
/// LINQ'te nested path'i her zaman <c>NULL</c> döner. Düz bir <c>PhaseName</c> ikizi eklemek
/// <b>yanlış yöne</b> başarısız olurdu: alan yeni olduğu için mevcut satırlarda yoktur, o
/// satırlar süzgece takılmaz ve kart eskileri SESSİZCE hiç göstermez — aranan kayıtlar tam
/// olarak eskiler olduğu için kart işe yaramazdı. Faz zaten türetilebilir: zincir varsa ve
/// kapanmamışsa saga tanımı gereği <c>TerminationInProgress</c>'tedir.</para>
/// </summary>
public static class StuckApprovalPolicy
{
    /// <param name="chain">Fesih onay zinciri; <c>null</c> ise fesih hiç istenmemiştir.</param>
    /// <param name="requestedAt">
    /// Talebin açıldığı an. <c>null</c> <b>tıkanmış</b> demektir — eksik veri sınırı
    /// gevşetemez (#252).
    /// </param>
    public static bool IsStuck(
        TerminationApprovalChain? chain, DateTime? requestedAt, DateTime now, int thresholdDays)
    {
        if (chain is null)
            return false;

        if (chain.IsCompleteOrOverridden())
            return false;

        if (requestedAt is null)
            return true;

        return requestedAt.Value <= now.AddDays(-thresholdDays);
    }

    /// <summary>Zincirin yaşı gün olarak; talep zamanı bilinmiyorsa <c>null</c>.</summary>
    public static int? AgeInDays(DateTime? requestedAt, DateTime now) =>
        requestedAt is { } at ? (int)(now - at).TotalDays : null;
}
