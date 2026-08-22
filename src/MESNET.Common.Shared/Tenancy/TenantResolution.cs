namespace MESNET.Common.Shared.Tenancy;

/// <summary>
/// Bir isteğin hangi kiracı adına çalışacağını belirler (#149).
///
/// <para><b>Kiracı = okul</b> (ADR-0003). Kaynak <c>institution_id</c> claim'idir ve o claim
/// artık yalnız sunucu tarafından üretilir — token'dan gelen değer kabul edilmiyor (ADR-0003
/// adım 2). Kiracı sınırının kullanıcının yazabileceği bir kaynaktan gelmemesi, izolasyonun
/// tamamının dayandığı varsayımdır.</para>
///
/// <para><b>Kapsamsız kullanıcıya kiracı UYDURULMAZ.</b> Kurumu olmayan bir kullanıcıyı
/// varsayılan ya da platform kiracısına düşürmek, onun yazmalarını sessizce yanlış bölmeye
/// göndermek olurdu — kiracılığın engellemek için var olduğu şeyin ta kendisi. Kiracı
/// çözülemezse <c>null</c> döner ve kiracıya ait veriye erişim <b>gürültülü biçimde</b>
/// başarısız olur.</para>
/// </summary>
public static class TenantResolution
{
    /// <summary>
    /// Hiçbir okula ait olmayan işlerin kiracısı: ulusal parametreler, alan/dal kataloğu,
    /// kimlik katmanı bakımı ve arka plan görevleri.
    ///
    /// <para><b>Bir okul değildir</b> ve kiracıya ait belge yazmaz. Var olma sebebi, kiracısız
    /// session'ın yasak olmasıdır (<c>DefaultTenantUsageEnabled = false</c>): kapsam dışı işler
    /// de bir kimlik altında çalışmak zorundadır ve o kimliğin <b>adı olmalıdır</b> —
    /// <c>*DEFAULT*</c> gibi "kimin olduğu belirsiz" bir kova değil.</para>
    /// </summary>
    public const string Platform = "platform";

    /// <summary>
    /// Kurum üstü katmanda çalışma yetkisi veren izin öneki. Bu önekteki bir izni olan kullanıcı
    /// (bugün yalnız <c>SystemAdmin</c>) hiçbir okula bağlı değildir ama ulusal parametreleri
    /// yazar; kiracısı <see cref="Platform"/>'dur.
    /// </summary>
    private const string PlatformPermissionPrefix = "platform:";

    /// <summary>
    /// Bir okul kimliğini kiracı kimliğine çevirir. <b>1:1 eşleşmenin yaşadığı TEK yer burasıdır</b>
    /// (#148).
    ///
    /// <para><b>Neden ayrı bir fonksiyon:</b> "kiracı = okul" bir <i>karar</i>dır (ADR-0003), veri
    /// tipi zorunluluğu değil. Çevrim iki ayrı yerde tekrarlanırsa (istek yolunda ve arka plan
    /// kiracı dizininde) ve biri değişip diğeri kalırsa, zamanlanmış işler <b>hiçbir verinin
    /// kullanmadığı</b> bir kiracıda koşar. Sonuç istisna değil <b>boş sonuç</b> olur: rapor
    /// üretilmez, maaş dönemi açılmaz, log temiz kalır.</para>
    ///
    /// <para>Kiracı ekseni bir gün okuldan başka bir şeye taşınırsa değişecek yer burasıdır;
    /// çağıranların hiçbiri okul kimliği taşıdığını varsaymaz.</para>
    /// </summary>
    public static string ForInstitution(Guid institutionId) => institutionId.ToString();

    /// <param name="institutionIdClaim">
    /// Çözülmüş kurum kapsamı — <c>null</c> ya da boş olabilir ve bu geçerli bir durumdur.
    /// </param>
    /// <param name="permissions">Kullanıcının etkin izinleri.</param>
    /// <returns>Kiracı kimliği; çözülemezse <c>null</c>.</returns>
    public static string? Resolve(Guid? institutionIdClaim, IEnumerable<string> permissions)
    {
        if (institutionIdClaim is { } institutionId && institutionId != Guid.Empty)
            return ForInstitution(institutionId);

        // Okulu olmayan ama kurum ÜSTÜ yetkisi olan kullanıcı platform kiracısında çalışır.
        // Sıra önemli: kurumu olan kullanıcı, platform izni de taşısa kendi okulunda kalır —
        // aksi hâlde okul müdürü ulusal katmana yazarken okul verisinden kopardı.
        return permissions.Any(p => p.StartsWith(PlatformPermissionPrefix, StringComparison.Ordinal))
            ? Platform
            : null;
    }
}
