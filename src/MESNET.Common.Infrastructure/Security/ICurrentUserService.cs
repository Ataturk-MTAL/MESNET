using MESNET.Common.Shared.Security;

namespace MESNET.Common.Infrastructure.Security;

public interface ICurrentUserService
{
    UserContext? GetCurrentUser();
    Guid GetUserId();
    string GetFullName();
    bool HasPermission(string permission);
    bool IsInRole(string role);
}
