namespace MESNET.Security.Core.Entities;

/// <summary>
/// Rol → DIRECT olarak atanabilecek yetki domain (prefix) kapsamı — YAPILANDIRILABILIR.
/// Tek document (singleton). Yönetici düzenler; ChangeUserPermissions guardrail'i bunu okur.
/// İlk kez okunduğunda kod default'larından (AssignablePermissionScope.Defaults) seed'lenir.
/// </summary>
public class PermissionScopeConfig
{
    public static readonly Guid SingletonId = Guid.Parse("5c0fe000-0000-0000-0000-000000000001");

    public Guid Id { get; set; } = SingletonId;

    // Rol adı → atanabilir yetki domain prefix'leri (ör. "company:", "*")
    public Dictionary<string, List<string>> AllowedDomainsByRole { get; set; } = new();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
