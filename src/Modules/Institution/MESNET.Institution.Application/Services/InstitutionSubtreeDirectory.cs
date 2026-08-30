using Marten;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
// "Institution" hem ad alanı hem tip adı olduğu için doğrudan kullanılamaz (CS0118).
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Services;

/// <summary>
/// <inheritdoc cref="IInstitutionSubtreeDirectory"/>
///
/// <para><b>Neden <see cref="TenantResolution.Platform"/> ile okunuyor:</b> <c>Institution</c>
/// <c>DocumentTenancyMap</c> içinde <b>kimlik katmanındadır</b> — kiracı damgası taşımaz, çünkü
/// kiracının kendisidir. Yine de bir ada ihtiyaç var: kiracısız session yasaktır.</para>
/// </summary>
public sealed class InstitutionSubtreeDirectory : IInstitutionSubtreeDirectory
{
    private readonly IDocumentStore _store;

    public InstitutionSubtreeDirectory(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
        string pathPrefix, CancellationToken cancellationToken = default)
    {
        // Boş önek "her şey" demek DEĞİLDİR. Marten string.StartsWith("") her satırı geçirirdi
        // ve kapsamlı bir aktör sessizce bütün okulları görürdü. Kapsamsız kalmak, kapsamı
        // aşmaktan iyidir.
        if (string.IsNullOrWhiteSpace(pathPrefix))
            return [];

        await using var session = _store.QuerySession(TenantResolution.Platform);

        // Marten string.StartsWith'i SQL'de LIKE 'önek%' çevirir; ham SQL ve WITH RECURSIVE
        // gerekmez. Yolu olmayan satır alt ağaçta DEĞİLDİR.
        var ids = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .Where(i => i.Path != null && i.Path.StartsWith(pathPrefix))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return ToTenants(ids);
    }

    public async Task<IReadOnlyList<string>> GetAllSchoolTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession(TenantResolution.Platform);

        var ids = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return ToTenants(ids);
    }

    // Çevrim burada TEKRARLANMAZ: 1:1 eşleşme TenantResolution'da tek noktada yaşar (#148).
    private static IReadOnlyList<string> ToTenants(IEnumerable<Guid> ids) =>
        ids.Select(TenantResolution.ForInstitution).ToList();
}
