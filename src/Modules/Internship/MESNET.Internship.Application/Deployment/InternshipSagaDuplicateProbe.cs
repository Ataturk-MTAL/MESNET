using Marten;
using MESNET.Common.Infrastructure.Deployment;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using MESNET.Internship.Application.Sagas;

namespace MESNET.Internship.Application.Deployment;

/// <summary>
/// <c>POST /api/internships/resync-sagas</c> koşturulmuş mu — <b>kopya saga</b> sayısından ölçer
/// (#248, #251).
///
/// <para><b>Belirti:</b> saga sayısı, o saga'ların işaret ettiği <b>tekil yerleştirme</b>
/// sayısından fazla. Saga kimliği eskiden <c>Guid.NewGuid()</c> ile üretiliyordu; yeniden
/// yayınlanan her <c>StudentPlaced</c> yeni bir saga doğuruyordu. Ölçüm (#251): 2248 saga, yalnız
/// 95 yerleştirme.</para>
///
/// <para><b>Neden kopya sayılıyor, neden "ContractId boş" değil:</b> boş <c>ContractId</c> meşru
/// bir durumdur — sözleşmesi henüz kurulmamış yeni yerleştirme öyle görünür. Kopya ise hiçbir
/// koşulda meşru değildir: bir yerleştirmenin bir saga'sı olur.</para>
///
/// <para><b>Sıra bozulmaz:</b> önce tekilleştirme, sonra bağlama. Kopyalar dururken sözleşme
/// bağlamak, 24 kardeşten rastgele birine bağlamak demektir.</para>
///
/// <para><b>Neden kiracı listesi + <c>TenantIsOneOf</c>:</b> <c>InternshipSaga</c> kiracı
/// damgalıdır. Tek bir kiracıda okumak yalnız o okulun kopyalarını görürdü; platform kiracısında
/// okumak <b>hiçbir satır</b> görmezdi — hata değil, sessiz sıfır.</para>
/// </summary>
public sealed class InternshipSagaDuplicateProbe(
    IDocumentStore store,
    ITenantDirectory tenantDirectory) : IDeploymentPrerequisiteProbe
{
    /// <summary>
    /// Tek koşuda okunacak en fazla saga kimliği. Açılışı bekleten bir sonda, doğrulamak istediği
    /// dağıtımın kendisini geciktirir. Sınıra dayanıldığında sonuç <b>sessizce kırpılmaz</b> —
    /// bulgunun metnine yazılır.
    /// </summary>
    private const int MaxScannedSagas = 20_000;

    public string Name => "Staj saga'sı kopyaları";

    public string Remedy =>
        "POST /api/internships/resync-sagas  →  ardından  POST /api/contracts/resync-internship-links"
        + "   (kiracı başına, platform:tenant:manage — SIRA BOZULMAZ)";

    public async Task<PrerequisiteFinding?> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await tenantDirectory.GetActiveTenantsAsync(cancellationToken);

        // Kiracı yoksa saga da yoktur. Boş listede sorgu HİÇ kurulmaz — parametresiz
        // TenantIsOneOf()'un SQL'de ne ürettiğine güvenilmez.
        if (tenants.Count == 0)
            return null;

        await using var session = store.QuerySession(TenantResolution.Platform);

        var tenantIds = tenants.ToArray();

        // Yalnız tek sütun okunur (Guid). SmartEnum alanı (Phase) projeksiyona ALINMAZ: Marten
        // onu data->'phase'->>'Name' olarak çevirir ve her zaman NULL döner.
        var placementIds = await session.Query<InternshipSaga>()
            .Where(s => s.TenantIsOneOf(tenantIds))
            .Select(s => s.PlacementId)
            .Take(MaxScannedSagas)
            .ToListAsync(cancellationToken);

        var scanned = placementIds.Count;
        if (scanned == 0)
            return null;

        var distinct = placementIds.Distinct().Count();
        var duplicates = scanned - distinct;

        if (duplicates == 0)
            return null;

        var truncated = scanned == MaxScannedSagas
            ? $" (tarama {MaxScannedSagas} saga'da durduruldu; gerçek sayı daha yüksek olabilir)"
            : string.Empty;

        return new PrerequisiteFinding(
            Symptom:
                $"{scanned} saga taranmış, yalnız {distinct} tekil yerleştirmeye işaret ediyor — "
                + $"{duplicates} kopya{truncated}.",
            Consequence:
                "Stajlar sözleşmeleriyle bağlanmaz ve AwaitingContract fazında çakılı kalır. "
                + "Fesih zinciri kopyalardan rastgele birine yazılır; devamsızlık eşiği aşıldığında "
                + "hangi saga'nın tetikleneceği belirsizdir.");
    }
}
