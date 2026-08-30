using Marten;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
// "Institution" hem ad alanı hem tip adı olduğu için doğrudan kullanılamaz (CS0118).
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Services;

/// <summary>
/// Kiracı listesini okul kayıtlarından üretir (#149). Kiracı = okul (ADR-0003), dolayısıyla
/// kiracı kimlikleri <c>Institution</c> belgelerinin kimlikleridir.
///
/// <para><b>Neden <see cref="TenantResolution.Platform"/> ile okunuyor:</b> <c>Institution</c>
/// <see cref="DocumentTenancyMap"/> içinde <b>kimlik katmanındadır</b> — kiracı damgası
/// taşımaz, çünkü kiracının kendisidir. Kiracıya göre süzülseydi hiçbir okul kendi kaydını
/// göremezdi. Yine de bir ada ihtiyaç var: kiracısız session yasaktır.</para>
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

        // KİRACI = OKUL. İl ve ilçe müdürlüğü düğümleri kiracı DEĞİLDİR ve kiracı damgalı
        // hiçbir veri taşımazlar. Süzülmeselerdi arka plan işleri hiçbir verinin bulunmadığı
        // "kiracılarda" koşardı — istisna değil, sessiz boş geçiş.
        var ids = await session.Query<InstitutionRecord>()
            .OfNodeType(InstitutionNodeType.School)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        // Çevrim burada TEKRARLANMAZ: 1:1 eşleşme TenantResolution'da tek noktada yaşar (#148).
        // Kopyalansaydı, istek yolu değişip burası kalınca arka plan işleri hiçbir verinin
        // kullanmadığı bir kiracıda koşardı — istisna değil, sessiz boş sonuç.
        return ids.Select(TenantResolution.ForInstitution).ToList();
    }
}
