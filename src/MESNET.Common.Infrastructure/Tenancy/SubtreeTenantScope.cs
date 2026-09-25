using MESNET.Common.Shared.Security;
using MESNET.Common.Shared.Tenancy;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Aktörün okuyabileceği okul kiracılarının listesini <b>üreten TEK yer</b> — kiracılar arası
/// okumanın kararı burada verilir.
///
/// <para><b>Üretim/uygulama ayrımı:</b> bu sınıf listeyi kurar, sorguya
/// <b>uygulamaz</b>. Liste <c>CrossTenantQuery.CollectAsync</c> ile OKUL BAŞINA session açılarak
/// okunur (#317) — <c>TenantIsOneOf</c> row-level security altında yalnız session'ın kiracısını
/// gösterdiği için artık hiç kullanılmaz. Bölünme kasıtlıdır: bu sınıf
/// <see cref="InstitutionVisibility"/>'yi kiracı kimliklerine çevirir, handler onları okur;
/// kiracılar arası okuyabilecek dosyalar <c>CrossTenantQueryDriftTests</c> ile kilitlidir.</para>
///
/// <para><b>Girdi güvenliği burada tektir:</b> kiracılar arası okuma yalıtımı bilerek aşar.
/// Serbest bırakılırsa bir gün biri onu <b>istekten gelen</b> kimliklerle çağırır ve kapsam
/// sessizce açılır — hata değil, fazla veri. Bu sınıf listeyi yalnız <see cref="InstitutionVisibility"/>'den üretir;
/// istekten gelen hiçbir değer buraya giremez. Handler tarafı bu listeyi olduğu gibi kullanır,
/// kendi kaynağını türetmez.</para>
///
/// <para><b>İki ayrı kaynak, tek karar:</b> kapsamsız (platform) aktör için liste
/// <see cref="ITenantDirectory.GetActiveTenantsAsync"/>'ten gelir — bu sorguyu zaten
/// <c>InstitutionTenantDirectory</c> barındırıyordu, aynısını <see cref="IInstitutionSubtreeDirectory"/>'de
/// tekrar tanımlamak yerine mevcut arayüz kullanılır. Yol önekli (il/ilçe) aktör için liste
/// <see cref="IInstitutionSubtreeDirectory.GetSchoolTenantsAsync"/> ile alt ağaca daraltılır —
/// bu ikisi farklı sorgulardır ve birleştirilemez.</para>
///
/// <para><b><c>AnyTenant()</c> bu depoda YASAKTIR</b> — kapsamsız aktör için bile
/// kullanılmaz, istisnasızdır. Kilitleyen test: <c>CrossTenantQueryDriftTests</c>.</para>
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
    /// Kiracı kimlikleri; kapsamsız aktörde <b>boş liste</b> — okunacak okul yoktur.
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
