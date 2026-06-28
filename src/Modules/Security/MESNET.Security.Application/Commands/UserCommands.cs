using MESNET.Common.Shared.Pagination;

namespace MESNET.Security.Application.Commands;

public sealed record CreateUser(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? TemporaryPassword,
    List<string> Roles,
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    Dictionary<string, string>? Metadata = null);

public sealed record UpdateUser(Guid UserAccountId, string Email, string FirstName, string LastName);

public sealed record ChangeUserRoles(Guid UserAccountId, List<string> NewRoles);

public sealed record ChangeUserPermissions(Guid UserAccountId, List<string> DirectPermissions);

public sealed record ToggleUserStatus(Guid UserAccountId, bool Enable, string? Reason = null);

public sealed record DeleteUser(Guid UserAccountId);

public sealed record GetUserAccounts(
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    string? Role = null,
    bool? IsEnabled = null) : PagedQuery;

public sealed record GetUserAccount(Guid UserAccountId);

/// <summary>Keycloak'taki tüm kullanıcıları lokal UserAccount read-model'ine senkronize eder (upsert).</summary>
public sealed record SyncUsersFromKeycloak;
