namespace MESNET.Security.Application.Commands;

/// <summary>Rol → atanabilir yetki domain kapsamını getir (config yoksa kod default'larıyla seed'li).</summary>
public sealed record GetPermissionScopes;

/// <summary>Rol → atanabilir yetki domain kapsamını güncelle (yönetici).</summary>
public sealed record UpdatePermissionScopes(Dictionary<string, List<string>> AllowedDomainsByRole);

/// <summary>UI için: roller, seçilebilir tüm domain'ler ve mevcut kapsam haritası.</summary>
public sealed record PermissionScopeDto(
    List<string> Roles,
    List<string> AllDomains,
    Dictionary<string, List<string>> AllowedDomainsByRole);
