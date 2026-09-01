using Marten;
using MESNET.Common.Infrastructure.Deployment;
using MESNET.Common.Shared.Tenancy;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ReadModels;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Deployment;

/// <summary>
/// <c>POST /api/security/users/replay</c> koşturulmuş mu — <c>InstitutionManagerLink</c>
/// görünümünün <b>hiç satırı olup olmadığından</b> ölçer.
///
/// <para><b>Belirti:</b> okul kaydı var ama görünümde sıfır satır. Görünüm Security modülünün
/// kullanıcı olaylarıyla <b>bundan sonra</b> beslenir; dağıtımdan önce var olan hesaplar için
/// satır hiç doğmaz.</para>
///
/// <para><b>Neden sıfır eşiği, neden "okul başına satır var mı" değil:</b> görünüm KULLANICI
/// başına satır tutar, kurum başına değil — okulla birebir eşleşme aranamaz. Ayrıca gerçekten
/// yöneticisi olmayan okul <b>meşru bir durumdur</b>; panonun var olma sebebi odur. Eksikliği
/// meşru durumdan ayıran tek ölçüt görünümün tümden boş olmasıdır.</para>
///
/// <para><b>Ölçüldü:</b> replay atlanınca pano <b>her okulu</b> yöneticisiz sayıyordu; hata
/// dönmüyor, log basılmıyordu — yalnız yanlış bir liste görünüyordu.</para>
/// </summary>
public sealed class InstitutionManagerLinkProbe(IDocumentStore store) : IDeploymentPrerequisiteProbe
{
    public string Name => "Yönetici bağı görünümü (müdürlük panosu)";

    public string Remedy => "POST /api/security/users/replay   (platform:tenant:manage)";

    public async Task<PrerequisiteFinding?> ProbeAsync(CancellationToken cancellationToken = default)
    {
        // InstitutionManagerLink de Institution gibi KİMLİK katmanındadır (kaynağı UserAccount,
        // hedefi Institution — ikisi de kiracı damgası taşımaz).
        await using var session = store.QuerySession(TenantResolution.Platform);

        var schoolCount = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .CountAsync(cancellationToken);

        // Okul yoksa pano da boştur; doldurulacak bir görünüm yok.
        if (schoolCount == 0)
            return null;

        var linkCount = await session.Query<InstitutionManagerLink>().CountAsync(cancellationToken);

        if (linkCount > 0)
            return null;

        return new PrerequisiteFinding(
            Symptom: $"{schoolCount} okul kayıtlı; InstitutionManagerLink görünümünde 0 satır var.",
            Consequence:
                "Müdürlük panosunun \"yöneticisiz okullar\" kartı HER okulu yöneticisiz sayar — "
                + "gerçekte müdürü olanlar dahil. Hata dönmez, log basılmaz; yalnız yanlış bir "
                + "liste görünür.");
    }
}
