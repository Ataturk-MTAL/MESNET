using System.Security.Claims;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Http;

namespace MESNET.Common.Infrastructure.Security;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private UserContext? _cachedContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserContext? GetCurrentUser()
    {
        if (_cachedContext is not null)
            return _cachedContext;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var userId = Guid.TryParse(user.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
        var fullName = user.FindFirst("name")?.Value
                       ?? user.FindFirst("preferred_username")?.Value
                       ?? "Bilinmeyen Kullanıcı";

        var institutionId = Guid.TryParse(user.FindFirst("institution_id")?.Value, out var instId) ? instId : (Guid?)null;
        var businessId = Guid.TryParse(user.FindFirst("business_id")?.Value, out var bizId) ? bizId : (Guid?)null;
        var studentId = Guid.TryParse(user.FindFirst("student_id")?.Value, out var stuId) ? stuId : (Guid?)null;

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = user.FindAll("permissions").Select(c => c.Value).ToList();
        var branchCodes = BranchCodeClaims.Read(user);

        _cachedContext = new UserContext(
            userId, fullName, institutionId, businessId, studentId, roles, permissions, branchCodes);
        return _cachedContext;
    }

    public Guid GetUserId()
    {
        return GetCurrentUser()?.UserId ?? Guid.Empty;
    }

    public string GetFullName()
    {
        return GetCurrentUser()?.FullName ?? "Bilinmeyen Kullanıcı";
    }

    public bool HasPermission(string permission)
    {
        var ctx = GetCurrentUser();
        if (ctx?.Permissions is null) return false;

        return ctx.Permissions.Any(p => RolePermissionMap.MatchesPermission(p, permission));
    }

    public bool IsInRole(string role)
    {
        var ctx = GetCurrentUser();
        if (ctx?.Roles is null) return false;

        return ctx.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetBranchCodes()
    {
        return GetCurrentUser()?.BranchCodes ?? [];
    }
}
