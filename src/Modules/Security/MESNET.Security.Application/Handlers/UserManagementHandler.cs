using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Microsoft.Extensions.Caching.Memory;

namespace MESNET.Security.Application.Handlers;

public static class CreateUserHandler
{
    public static async Task<(Result<Guid>, UserCreated?)> Handle(
        CreateUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        // Keycloak'ta kullanıcı oluştur
        var kcResult = await keycloak.CreateUserAsync(
            command.Username, command.Email, command.FirstName, command.LastName,
            command.TemporaryPassword);

        if (kcResult.IsFailure)
            return (Result<Guid>.Failure(kcResult.Error), null);

        var keycloakUserId = kcResult.Value;

        // Roller ata
        if (command.Roles.Count > 0)
        {
            var roleResult = await keycloak.AssignRealmRolesAsync(keycloakUserId, command.Roles);
            if (roleResult.IsFailure)
                return (Result<Guid>.Failure(roleResult.Error), null);
        }

        // User attributes ata (institution_id, business_id)
        var attributes = new Dictionary<string, string>();
        if (command.InstitutionId.HasValue)
            attributes["institution_id"] = command.InstitutionId.Value.ToString();
        if (command.BusinessId.HasValue)
            attributes["business_id"] = command.BusinessId.Value.ToString();

        if (attributes.Count > 0)
            await keycloak.SetUserAttributesAsync(keycloakUserId, attributes);

        // Lokal UserAccount kaydet
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            Username = command.Username,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Roles = command.Roles,
            InstitutionId = command.InstitutionId,
            BusinessId = command.BusinessId
        };

        session.Store(account);

        var @event = new UserCreated(
            account.Id, keycloakUserId, command.Username, account.FullName, command.Email,
            command.Roles, command.InstitutionId, command.BusinessId,
            command.Metadata ?? []);

        return (Result<Guid>.Success(account.Id), @event);
    }
}

public static class UpdateUserHandler
{
    public static async Task<(Result, UserUpdated?)> Handle(
        UpdateUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            return (Result.Failure(SecurityErrors.UserNotFound(command.UserAccountId)), null);

        var kcResult = await keycloak.UpdateUserAsync(
            account.KeycloakUserId, command.Email, command.FirstName, command.LastName);

        if (kcResult.IsFailure)
            return (kcResult, null);

        account.Email = command.Email;
        account.FirstName = command.FirstName;
        account.LastName = command.LastName;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        return (Result.Success(), new UserUpdated(
            account.Id, account.KeycloakUserId, account.FullName, command.Email));
    }
}

public static class ChangeUserRolesHandler
{
    public static async Task<(Result, UserRolesChanged?)> Handle(
        ChangeUserRoles command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            return (Result.Failure(SecurityErrors.UserNotFound(command.UserAccountId)), null);

        var previousRoles = account.Roles.ToList();

        // Silinecek roller
        var rolesToRemove = previousRoles.Except(command.NewRoles).ToList();
        if (rolesToRemove.Count > 0)
        {
            var removeResult = await keycloak.RemoveRealmRolesAsync(account.KeycloakUserId, rolesToRemove);
            if (removeResult.IsFailure)
                return (removeResult, null);
        }

        // Eklenecek roller
        var rolesToAdd = command.NewRoles.Except(previousRoles).ToList();
        if (rolesToAdd.Count > 0)
        {
            var addResult = await keycloak.AssignRealmRolesAsync(account.KeycloakUserId, rolesToAdd);
            if (addResult.IsFailure)
                return (addResult, null);
        }

        account.Roles = command.NewRoles;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        // Permission cache invalidate
        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return (Result.Success(), new UserRolesChanged(
            account.Id, account.KeycloakUserId, previousRoles, command.NewRoles));
    }
}

public static class ChangeUserPermissionsHandler
{
    public static async Task<(Result, UserPermissionsChanged?)> Handle(
        ChangeUserPermissions command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            return (Result.Failure(SecurityErrors.UserNotFound(command.UserAccountId)), null);

        // Keycloak user attributes'a direct_permissions yaz
        var attributes = new Dictionary<string, string>
        {
            ["direct_permissions"] = string.Join(",", command.DirectPermissions)
        };
        var kcResult = await keycloak.SetUserAttributesAsync(account.KeycloakUserId, attributes);
        if (kcResult.IsFailure)
            return (kcResult, null);

        account.DirectPermissions = command.DirectPermissions;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        // Permission cache invalidate
        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return (Result.Success(), new UserPermissionsChanged(
            account.Id, account.KeycloakUserId, command.DirectPermissions));
    }
}

public static class ToggleUserStatusHandler
{
    public static async Task<(Result, object?)> Handle(
        ToggleUserStatus command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            return (Result.Failure(SecurityErrors.UserNotFound(command.UserAccountId)), null);

        var kcResult = await keycloak.SetUserEnabledAsync(account.KeycloakUserId, command.Enable);
        if (kcResult.IsFailure)
            return (kcResult, null);

        account.IsEnabled = command.Enable;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        // Permission cache invalidate
        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        object @event = command.Enable
            ? new UserActivated(account.Id, account.KeycloakUserId)
            : new UserDeactivated(account.Id, account.KeycloakUserId, command.Reason ?? "");

        return (Result.Success(), @event);
    }
}

public static class DeleteUserHandler
{
    public static async Task<(Result, UserDeleted?)> Handle(
        DeleteUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            return (Result.Failure(SecurityErrors.UserNotFound(command.UserAccountId)), null);

        var kcResult = await keycloak.DeleteUserAsync(account.KeycloakUserId);
        if (kcResult.IsFailure)
            return (kcResult, null);

        session.Delete(account);

        return (Result.Success(), new UserDeleted(account.Id, account.KeycloakUserId));
    }
}
