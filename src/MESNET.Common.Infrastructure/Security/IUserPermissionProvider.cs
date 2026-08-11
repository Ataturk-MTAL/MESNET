namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// Kullanıcının güncel permission bilgisini sağlar.
/// Common.Infrastructure'da interface tanımlanır, Security.Application'da implement edilir.
/// Bu sayede döngüsel bağımlılık önlenir.
/// PermissionClaimsTransformation tarafından kullanılır.
/// </summary>
public interface IUserPermissionProvider
{
    Task<UserPermissionInfo?> GetUserPermissionInfoAsync(string keycloakUserId);
}

/// <param name="BranchCodes">
/// Kullanıcı kaydında girilen alan (branş) kodları (#126). Kayıt sırasında sabitlenir;
/// boş olabilir ve bu bir eksiklik değildir (müdür/müdür yardımcısı alana bağlı değildir).
/// </param>
/// <param name="InstitutionId">
/// Kullanıcının bağlı olduğu kurum — <b>kiracı anahtarı</b> (ADR-0003). Kayıt <b>tek</b>
/// otoritedir: token'daki <c>institution_id</c> claim'i her durumda atılır, kayıt boş olsa
/// bile. <c>BranchCodes</c>'tan daha katıdır; orada kayıt boşken token yedeği hâlâ kabul
/// edilir, burada edilmez. Boş olması geçerlidir (ör. sistem yöneticisi) ve kullanıcı o
/// hâlde kurum kapsamsız kalır.
/// </param>
/// <param name="LinkedStudentIds">
/// Velinin bağlı olduğu öğrenciler (#174). <b>Kayıt otoriterdir</b> — <c>BranchCodes</c> ile
/// birebir aynı güven sırası: doluysa token'daki <c>linked_student_ids</c> claim'leri atılır.
/// Boş olması normaldir (veli olmayan her kullanıcıda boştur) ve hiçbir öğrenciye erişim
/// doğurmaz.
/// </param>
/// <param name="RolesWrittenAt">
/// Kaydın son yazılma anı (<c>UpdatedAt ?? CreatedAt</c>) — sapma tespitinin zaman koşulu
/// için (#208). Token bundan SONRA üretilmişse Keycloak kayıttan sonra değişmiş demektir;
/// bu ayrım olmadan uygulamadan yapılan her rol kaldırma işlemi token ömrü boyunca yanlış
/// alarm üretirdi. Varsayılan <c>default</c> = "bilinmiyor"; o hâlde sapma iddia edilmez.
/// </param>
public sealed record UserPermissionInfo(
    bool IsEnabled,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> DirectPermissions,
    IReadOnlyList<string> BranchCodes,
    Guid? InstitutionId = null,
    /// <summary>İşletme kapsamı — otorite kayıttır, token değil (#229).</summary>
    Guid? BusinessId = null,
    /// <summary>Öğrencinin kendi profili — otorite kayıttır, token değil (#230).</summary>
    Guid? StudentId = null,
    IReadOnlyList<Guid>? LinkedStudentIds = null,
    DateTime RolesWrittenAt = default);
