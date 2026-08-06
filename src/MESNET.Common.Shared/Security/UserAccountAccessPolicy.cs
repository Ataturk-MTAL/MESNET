namespace MESNET.Common.Shared.Security;

/// <summary>
/// Bir <c>UserAccount</c> kaydının erişim üretip üretmediğine karar verir (#210).
///
/// <para><b>Neden ayrı bir karar:</b> "erişimi kes" için iki yol var — pasife alma ve silme —
/// ve bunlar farklı alanlara yazıyor. Karar tek yerde toplanmazsa biri korunur, öteki unutulur.
/// Gerçekten böyle oldu: <c>IsEnabled</c> kontrolü vardı, silme onun <b>yanından geçiyordu</b>.</para>
///
/// <para><b>Silmenin neden özel bir tehlikesi var:</b> <c>DeleteUser</c> kaydı gerçekten
/// siliyordu. Kayıt yoksa <c>PermissionClaimsTransformation</c> "bu kullanıcının kaydı henüz
/// oluşturulmamış" sanıp <b>token yedeğine</b> düşer ve izinleri <c>realm_access</c> rollerinden
/// yeniden türetir. Token imzalıdır, API onu yalnız imza + <c>exp</c> ile doğrular (introspection
/// yok) — yani silinen kullanıcı, token'ı sona erene kadar <b>tam yetkiyle</b> çalışırdı.
/// Realm'de <c>accessTokenLifespan: 1800</c>, yani 30 dakika.</para>
///
/// <para>Bu yüzden kayıt <b>silinmez, mezar taşı bırakılır</b>: kayıt bulunmaya devam eder,
/// karar buradan geçer ve yedek yoluna hiç düşülmez. Sert silmeye dönülmesini
/// <c>UserAccountAccessPolicyTests.UserAccount_sert_silinmiyor</c> engeller.</para>
/// </summary>
public static class UserAccountAccessPolicy
{
    /// <summary>
    /// Kayıt erişim üretiyor mu?
    /// </summary>
    /// <param name="isEnabled">Hesap etkin mi (pasife alma).</param>
    /// <param name="deletedAt">Silinme damgası; <c>null</c> = silinmemiş.</param>
    /// <remarks>
    /// İki alan birbirinin yedeği <b>değildir</b>, ayrı ayrı kapatır: silme sırasında
    /// <c>IsEnabled</c>'ı düşürmek unutulsa bile erişim doğmaz.
    /// </remarks>
    public static bool GrantsAccess(bool isEnabled, DateTime? deletedAt) =>
        isEnabled && deletedAt is null;
}
