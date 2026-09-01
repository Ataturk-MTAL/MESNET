using Marten;
using MESNET.Common.Infrastructure.Deployment;
using MESNET.Common.Shared.Tenancy;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
// "Institution" hem ad alanı hem tip adı olduğu için doğrudan kullanılamaz (CS0118).
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Deployment;

/// <summary>
/// <c>POST /api/institutions/rebuild-hierarchy</c> koşturulmuş mu — <b>yol alanından</b> ölçer.
///
/// <para><b>Belirti:</b> okul kaydı var ama <c>Path</c> boş. Ağaç kurulmadan <c>Path</c> hiçbir
/// yoldan dolmaz: kurum oluşturma ucu il/ilçe düğümü açmaz, yalnız bu adım açar.</para>
///
/// <para><b>Neden yalnız okul sayılıyor:</b> il ve ilçe müdürlüğü düğümlerinin kendisi bu adımda
/// <b>doğar</b>. Onları da sayan bir kontrol, adım hiç koşmadığında bölen sıfır olduğu için
/// eksikliği göremezdi.</para>
///
/// <para><b>Neden <see cref="TenantResolution.Platform"/>:</b> <c>Institution</c> kimlik
/// katmanındadır, kiracı damgası taşımaz — kiracıya göre süzülseydi hiçbir okul kendi kaydını
/// göremezdi. Yine de bir ada ihtiyaç var: kiracısız session yasaktır.</para>
/// </summary>
public sealed class InstitutionHierarchyProbe(IDocumentStore store) : IDeploymentPrerequisiteProbe
{
    public string Name => "Kurum ağacı (il/ilçe düğümleri ve yol)";

    public string Remedy => "POST /api/institutions/rebuild-hierarchy   (platform:tenant:manage)";

    public async Task<PrerequisiteFinding?> ProbeAsync(CancellationToken cancellationToken = default)
    {
        await using var session = store.QuerySession(TenantResolution.Platform);

        var schools = session.Query<InstitutionRecord>().OfNodeType(InstitutionNodeType.School);

        var schoolCount = await schools.CountAsync(cancellationToken);

        // Hiç okul yoksa kurulacak ağaç da yoktur. Boş bir kurulum eksiklik DEĞİLDİR.
        if (schoolCount == 0)
            return null;

        var withoutPath = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .Where(i => i.Path == null)
            .CountAsync(cancellationToken);

        if (withoutPath == 0)
            return null;

        return new PrerequisiteFinding(
            Symptom: $"{schoolCount} okul kayıtlı; {withoutPath} tanesinin ağaç yolu (Path) boş.",
            Consequence:
                "Müdürlük kapsamı yol önekiyle çözülür. Yolu olmayan okul hiçbir alt ağaçta "
                + "görünmez: il/ilçe yetkilisi kendi okullarını boş liste olarak görür, hata almaz.");
    }
}
