using Marten;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
// "Institution" hem ad alanı hem tip adı olduğu için doğrudan kullanılamaz (CS0118).
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Services;

/// <summary>
/// Kiracı listesini okul kayıtlarından üretir (#149). Kiracı = okul (ADR-0003), dolayısıyla
/// kiracı kimlikleri <c>Institution</c> belgelerinin kimlikleridir.
///
/// <para><b>Neden <see cref="TenantResolution.Platform"/> ile okunuyor:</b> <c>Institution</c>
/// <see cref="DocumentTenancyMap"/> içinde <b>paylaşımlı</b>dır — kiracı damgası taşımaz, çünkü
/// kiracının kendisidir. Kiracıya göre süzülseydi hiçbir okul kendi kaydını göremezdi. Yine de
/// bir ada ihtiyaç var: kiracısız session yasaktır.</para>
/// </summary>
public sealed class InstitutionTenantDirectory : ITenantDirectory
{
    private readonly IDocumentStore _store;

    public InstitutionTenantDirectory(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<string>> GetActiveTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession(TenantResolution.Platform);

        var ids = await session.Query<InstitutionRecord>()
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        return ids.Select(id => id.ToString()).ToList();
    }
}
