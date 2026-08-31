using MESNET.Common.Shared.Security;

namespace MESNET.Institution.Core.ReadModels;

/// <summary>
/// Bir kullanıcının kurum bağı ve yönetme yetkisi — Institution modülünün kendi şemasındaki
/// yerel görünüm (D2).
///
/// <para><b>Neden KULLANICI başına, kurum başına sayaç değil:</b> sayacı azaltması gereken
/// olaylar (<c>UserRolesChanged</c>, <c>UserDeactivated</c>, <c>UserDeleted</c>) kurum kimliği
/// <b>taşımaz</b>. Institution modülü rolü değişen kullanıcının hangi okula bağlı olduğunu
/// bilemez, dolayısıyla hangi satırdan düşeceğini de bilemez. Kullanıcı başına satırda her
/// olay tek bir kullanıcının durumunu <b>mutlak</b> olarak yazar; artırma/azaltma yoktur,
/// kayan sayaç da yoktur.</para>
///
/// <para><b>Neden Security'ye sorulmuyor:</b> <c>UserAccount</c> Security modülünün
/// şemasındadır ve başka modülün oraya sorgu atması yasaktır (şema izolasyonu). Bilgi
/// olaylarla taşınır.</para>
/// </summary>
public sealed class InstitutionManagerLink
{
    /// <summary>Kullanıcı hesabı kimliği — belge kimliği olarak kullanılır.</summary>
    public Guid Id { get; set; }

    /// <summary><c>null</c> = kullanıcı hiçbir kuruma bağlı değil.</summary>
    public Guid? InstitutionId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool HasManagePermission { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Roller <c>institution:manage</c> veriyor mu.
    ///
    /// <para><b>Rol adına BAKILMAZ</b> (ADR-0001): karar izne bakar.
    /// <c>RolePermissionMap.GetPermissionsForRoles</c> wildcard'ları
    /// (<c>institution:*</c>) zaten genişletir.</para>
    /// </summary>
    public static bool HasManage(IEnumerable<string> roles) =>
        RolePermissionMap.GetPermissionsForRoles(roles)
            .Contains(Permissions.Institution.Manage, StringComparer.OrdinalIgnoreCase);
}
