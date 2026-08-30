using MESNET.Common.Shared.Security;
using MESNET.Common.Shared.Tenancy;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Aktörün okuyabileceği okul kiracılarının listesi — <b>kiracılar arası okumanın TEK
/// kapısı</b>.
///
/// <para><b>Neden tek kapı:</b> Marten'in <c>TenantIsOneOf(...)</c> operatörü kiracı yalıtımını
/// bilerek deler; ürettiği SQL <c>tenant_id IN (...)</c>'dir. Serbest bırakılırsa bir gün biri
/// onu <b>istekten gelen</b> kimliklerle çağırır ve kapsam sessizce açılır — hata değil, fazla
/// veri. Bu sınıf listeyi yalnız <see cref="InstitutionVisibility"/>'den üretir; istekten gelen
/// hiçbir değer buraya giremez.</para>
///
/// <para><b>İki ayrı kaynak, tek karar:</b> kapsamsız (platform) aktör için liste
/// <see cref="ITenantDirectory.GetActiveTenantsAsync"/>'ten gelir — bu sorguyu zaten
/// <c>InstitutionTenantDirectory</c> barındırıyordu, aynısını <see cref="IInstitutionSubtreeDirectory"/>'de
/// tekrar tanımlamak yerine mevcut arayüz kullanılır. Yol önekli (il/ilçe) aktör için liste
/// <see cref="IInstitutionSubtreeDirectory.GetSchoolTenantsAsync"/> ile alt ağaca daraltılır —
/// bu ikisi farklı sorgulardır ve birleştirilemez.</para>
///
/// <para><b><c>AnyTenant()</c> bu depoda YASAKTIR</b> — kapsamsız aktör için bile
/// kullanılmaz. Tek kod yolu, tek gözden geçirme noktası. Kilitleyen test:
/// <c>CrossTenantQueryDriftTests</c>.</para>
/// </summary>
public sealed class SubtreeTenantScope
{
    private readonly IInstitutionSubtreeDirectory _directory;
    private readonly ITenantDirectory _tenantDirectory;

    public SubtreeTenantScope(IInstitutionSubtreeDirectory directory, ITenantDirectory tenantDirectory)
    {
        _directory = directory;
        _tenantDirectory = tenantDirectory;
    }

    /// <summary>
    /// Kapsamı kiracı kimliklerine çevirir.
    /// </summary>
    /// <returns>
    /// Kiracı kimlikleri; kapsamsız aktörde <b>boş liste</b>. Çağıran boş listede sorguyu HİÇ
    /// kurmamalıdır — parametresiz <c>TenantIsOneOf()</c>'un davranışına güvenilmez.
    /// </returns>
    public async Task<IReadOnlyList<string>> ResolveAsync(
        InstitutionVisibility scope, CancellationToken cancellationToken = default)
    {
        if (scope.Unrestricted)
            return await _tenantDirectory.GetActiveTenantsAsync(cancellationToken);

        if (scope.PathPrefix is { } prefix && !string.IsNullOrWhiteSpace(prefix))
            return await _directory.GetSchoolTenantsAsync(prefix, cancellationToken);

        // Okul aktörü kendi kiracısını bilir; dizine gitmeye gerek yok.
        if (scope.InstitutionId is { } institutionId && institutionId != Guid.Empty)
            return [TenantResolution.ForInstitution(institutionId)];

        // Kapsamsız aktör: her şeyi görmek yerine hiçbir şey görmek.
        return [];
    }
}
