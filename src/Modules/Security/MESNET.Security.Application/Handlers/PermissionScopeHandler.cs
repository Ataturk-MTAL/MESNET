using Marten;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Application.Handlers;

public static class PermissionScopeHandler
{
    public static async Task<PermissionScopeDto> Handle(GetPermissionScopes _, IQuerySession session)
    {
        var config = await session.LoadAsync<PermissionScopeConfig>(PermissionScopeConfig.SingletonId);

        // Config yoksa kod default'larından başla
        var map = config?.AllowedDomainsByRole
            ?? AssignablePermissionScope.Defaults.ToDictionary(k => k.Key, v => v.Value.ToList());

        // Tüm roller görünsün (yenisi varsa default'uyla ya da boşla)
        foreach (var role in MesnetRoles.All)
            map.TryAdd(role,
                AssignablePermissionScope.Defaults.TryGetValue(role, out var d) ? d.ToList() : []);

        var allDomains = new List<string> { AssignablePermissionScope.All };
        allDomains.AddRange(AssignablePermissionScope.AllDomains);

        return new PermissionScopeDto(MesnetRoles.All.ToList(), allDomains, map);
    }

    public static async Task Handle(UpdatePermissionScopes command, IDocumentSession session)
    {
        var config = await session.LoadAsync<PermissionScopeConfig>(PermissionScopeConfig.SingletonId)
                     ?? new PermissionScopeConfig();

        config.AllowedDomainsByRole = command.AllowedDomainsByRole;
        config.UpdatedAt = DateTime.UtcNow;
        session.Store(config);
    }

    /// <summary>ChangeUserPermissions guardrail'inin kullandığı, config-aware kapsam haritası.</summary>
    public static async Task<IReadOnlyDictionary<string, string[]>> LoadScopeAsync(IDocumentSession session)
    {
        var config = await session.LoadAsync<PermissionScopeConfig>(PermissionScopeConfig.SingletonId);
        return config?.AllowedDomainsByRole.ToDictionary(k => k.Key, v => v.Value.ToArray())
               ?? AssignablePermissionScope.Defaults.ToDictionary(k => k.Key, v => v.Value);
    }
}
