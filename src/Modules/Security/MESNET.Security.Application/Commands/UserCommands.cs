using MESNET.Common.Shared.Pagination;

namespace MESNET.Security.Application.Commands;

/// <param name="BranchCodes">
/// Kullanıcının sorumlu olduğu alan (branş) kodları (#126) — <c>InstitutionId</c> /
/// <c>BusinessId</c> ile aynı desende birinci sınıf alandır, <c>Metadata</c> sözlüğünden
/// okunmaz. `branch_codes` Keycloak özniteliği ve token claim'i buradan üretilir.
///
/// <para>Boş bırakılabilir: müdür/müdür yardımcısı hiçbir alana bağlı değildir. Zorunluluk
/// rol adından değil permission'dan türetilir — bkz. <c>CreateUserValidator</c>.</para>
/// </param>
public sealed record CreateUser(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? TemporaryPassword,
    List<string> Roles,
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    Dictionary<string, string>? Metadata = null,
    List<string>? BranchCodes = null);

public sealed record UpdateUser(Guid UserAccountId, string Email, string FirstName, string LastName);

public sealed record ChangeUserRoles(Guid UserAccountId, List<string> NewRoles);

/// <summary>
/// Kullanıcının alan (branş) kapsamını değiştirir (#126).
///
/// <para><b>Neden ayrı komut, <c>UpdateUser</c>'a alan eklemek yerine:</b> <c>UpdateUser</c>
/// kimlik bilgisi (ad/soyad/e-posta) günceller ve <c>user:update</c> ile korunur; alan kapsamı
/// ise bir <b>yetki kapsamı</b> kararıdır ve <c>user:roles:manage</c> ile korunmalıdır — tıpkı
/// <c>ChangeUserRoles</c> / <c>ChangeUserPermissions</c> gibi. Ayrıca kapsam değişimi permission
/// cache'ini geçersiz kılmak zorundadır; kimlik güncellemesi kılmaz. İki farklı yetki seviyesini
/// tek komutta birleştirmek, ad değiştirebilen bir kullanıcıya kapsam değiştirme yolu açardı.</para>
/// </summary>
public sealed record ChangeUserBranches(Guid UserAccountId, List<string> BranchCodes);

/// <summary>
/// Velinin bağlı olduğu öğrencileri değiştirir (#174).
///
/// <para><b>Neden ayrı komut:</b> <c>ChangeUserBranches</c> ile aynı gerekçe — bu bir
/// <b>yetki kapsamı</b> kararıdır, kimlik güncellemesi değil. Bağ kaydı velinin hangi
/// öğrencinin verisine erişeceğini belirler; izinleri değil kapsamı açar (ADR-0001).</para>
///
/// <para><b>Boş liste geçerlidir</b> — bağı kaldırmak meşru bir işlemdir (öğrenci mezun oldu,
/// vesayet değişti). Kaldırma anında kapsam kapanır: veli artık hiçbir öğrenciye erişemez.</para>
/// </summary>
public sealed record ChangeUserStudents(Guid UserAccountId, List<Guid> StudentIds);

/// <summary>
/// Kullanıcının kurum (kiracı) bağını değiştirir (ADR-0003 adım 2).
///
/// <para><b>Neden ayrı komut:</b> kurum bağı artık <b>tek</b> yerden değişir. Token'daki
/// <c>institution_id</c> claim'i hiç kabul edilmiyor ve <c>SyncUsersFromKeycloak</c> de bu alanı
/// yazmıyor; geriye idari bir işlem kalıyor. <c>UpdateUser</c>'a alan eklemek, ad-soyad
/// değiştirebilen bir kullanıcıya <b>kiracı anahtarı</b> yazma yolu açardı.</para>
///
/// <para><b><c>null</c> geçerli bir değerdir</b> — bağı çözmek meşru bir işlemdir (kurumdan
/// ayrılan personel, yanlış kuruma bağlanmış hesap). Çözülen kullanıcı kapsamsız kalır ve
/// kurum kapsamı isteyen uçlar ona kapanır.</para>
///
/// <para>Kapsam kararı <c>UserInstitutionScopePolicy</c> içindedir; aktörün kurumu istekten
/// değil token claim'inden okunur.</para>
/// </summary>
public sealed record ChangeUserInstitution(Guid UserAccountId, Guid? InstitutionId)
{
    /// <summary>İşlemi yapanın kurum kapsamı — uçta token claim'inden doldurulur, istekten ALINMAZ.</summary>
    public Guid? ActorInstitutionId { get; init; }
}

public sealed record ChangeUserPermissions(Guid UserAccountId, List<string> DirectPermissions);

public sealed record ToggleUserStatus(Guid UserAccountId, bool Enable, string? Reason = null);

public sealed record DeleteUser(Guid UserAccountId);

/// <param name="MissingBranchOnly">
/// Yalnız <b>alan kodu beklenen ama girilmemiş</b> kullanıcıları listeler (#126).
/// Rol değişimiyle alan şefi yapılıp branşsız kalan kullanıcıları idarenin görmesi içindir.
/// Muafiyeti olan (müdür/müdür yrd.) kullanıcılar bu listeye ASLA girmez — onlarda boş
/// liste beklenen normal durumdur.
/// </param>
public sealed record GetUserAccounts(
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    string? Role = null,
    bool? IsEnabled = null,
    bool? MissingBranchOnly = null) : PagedQuery;

public sealed record GetUserAccount(Guid UserAccountId);

/// <summary>Keycloak'taki tüm kullanıcıları lokal UserAccount read-model'ine senkronize eder (upsert).</summary>
public sealed record SyncUsersFromKeycloak;

/// <summary>
/// Her <c>UserAccount</c> için <c>UserDisplayNameUpserted</c> anlık görüntüsünü yeniden yayınlar (#137).
///
/// <para>Denetim alanları yalnız kullanıcı kimliğini saklar; adı her modül kendi şemasındaki
/// <c>UserNameView</c>'ından çözer ve o view salt olayla beslenir. Bu komuttan önce var olan
/// hesaplar için hiç olay yayınlanmadığından view onlarsız kalır — bu uç o boşluğu doldurur.</para>
///
/// <para>Tekrar çalıştırmak güvenlidir: aynı kimlikle üzerine yazar, kopya üretmez.</para>
/// </summary>
public sealed record ResyncUserDisplayNames;
