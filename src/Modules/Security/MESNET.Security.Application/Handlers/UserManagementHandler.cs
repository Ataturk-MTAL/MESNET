using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Microsoft.Extensions.Caching.Memory;

namespace MESNET.Security.Application.Handlers;

public static class CreateUserHandler
{
    public static async Task<(Guid, UserCreated)> Handle(
        CreateUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var kcResult = await keycloak.CreateUserAsync(
            command.Username, command.Email, command.FirstName, command.LastName,
            command.TemporaryPassword);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        var keycloakUserId = kcResult.Value;

        if (command.Roles.Count > 0)
        {
            var roleResult = await keycloak.AssignRealmRolesAsync(keycloakUserId, command.Roles);
            if (roleResult.IsFailure)
                throw new DomainException(roleResult.Error);
        }

        var attributes = new Dictionary<string, string>();
        if (command.InstitutionId.HasValue)
            attributes["institution_id"] = command.InstitutionId.Value.ToString();
        if (command.BusinessId.HasValue)
            attributes["business_id"] = command.BusinessId.Value.ToString();

        if (attributes.Count > 0)
            await keycloak.SetUserAttributesAsync(keycloakUserId, attributes);

        // Alan (branş) kapsamı kayıt sırasında sabitlenir (#126). Boş liste geçerlidir —
        // müdür/müdür yardımcısı hiçbir alana bağlı değildir; öznitelik hiç yazılmaz.
        var branchCodes = NormalizeBranchCodes(command.BranchCodes);
        if (branchCodes.Count > 0)
            await keycloak.SetUserAttributeValuesAsync(keycloakUserId, BranchCodeClaims.ClaimType, branchCodes);

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
            BusinessId = command.BusinessId,
            BranchCodes = branchCodes
        };

        session.Store(account);

        var @event = new UserCreated(
            account.Id, keycloakUserId, command.Username, account.FullName, command.Email,
            command.Roles, command.InstitutionId, command.BusinessId,
            command.Metadata ?? []);

        return (account.Id, @event);
    }

    /// <summary>Boş/yinelenen kodları eler. Sonuç boş olabilir — bu bir hata değildir.</summary>
    internal static List<string> NormalizeBranchCodes(IEnumerable<string>? codes) =>
        (codes ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

/// <summary>
/// Kullanıcının alan (branş) kapsamını değiştirir (#126).
///
/// <para>Kayıt sırasında girilen bilgi sonradan değişebilir: kullanıcı alan şefi yapılabilir,
/// başka alana geçebilir ya da ikinci bir alandan sorumlu olabilir. Kapsam değişimi permission
/// cache'ini geçersiz kılar — aksi hâlde eski kapsam 5 dakika daha geçerli kalırdı.</para>
/// </summary>
public static class ChangeUserBranchesHandler
{
    public static async Task<UserBranchesChanged> Handle(
        ChangeUserBranches command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var previous = account.BranchCodes.ToList();
        var branchCodes = CreateUserHandler.NormalizeBranchCodes(command.BranchCodes);

        // Boş liste özniteliği siler — kapsamı kaldırmak geçerli bir işlemdir
        // (ör. alan şefliğinden müdür yardımcılığına geçen kullanıcı).
        var kcResult = await keycloak.SetUserAttributeValuesAsync(
            account.KeycloakUserId, BranchCodeClaims.ClaimType, branchCodes);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.BranchCodes = branchCodes;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserBranchesChanged(account.Id, account.KeycloakUserId, previous, branchCodes);
    }
}

public static class UpdateUserHandler
{
    public static async Task<UserUpdated> Handle(
        UpdateUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var kcResult = await keycloak.UpdateUserAsync(
            account.KeycloakUserId, command.Email, command.FirstName, command.LastName);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.Email = command.Email;
        account.FirstName = command.FirstName;
        account.LastName = command.LastName;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        return new UserUpdated(account.Id, account.KeycloakUserId, account.FullName, command.Email);
    }
}

public static class ChangeUserRolesHandler
{
    public static async Task<UserRolesChanged> Handle(
        ChangeUserRoles command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var previousRoles = account.Roles.ToList();

        var rolesToRemove = previousRoles.Except(command.NewRoles).ToList();
        if (rolesToRemove.Count > 0)
        {
            var removeResult = await keycloak.RemoveRealmRolesAsync(account.KeycloakUserId, rolesToRemove);
            if (removeResult.IsFailure)
                throw new DomainException(removeResult.Error);
        }

        var rolesToAdd = command.NewRoles.Except(previousRoles).ToList();
        if (rolesToAdd.Count > 0)
        {
            var addResult = await keycloak.AssignRealmRolesAsync(account.KeycloakUserId, rolesToAdd);
            if (addResult.IsFailure)
                throw new DomainException(addResult.Error);
        }

        account.Roles = command.NewRoles;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserRolesChanged(account.Id, account.KeycloakUserId, previousRoles, command.NewRoles);
    }
}

public static class ChangeUserPermissionsHandler
{
    public static async Task<UserPermissionsChanged> Handle(
        ChangeUserPermissions command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        // Guardrail — kullanıcının rol kapsamı dışındaki yetkiler direct olarak ATANAMAZ
        // (ör. işletme kullanıcısına kurum-yönetimi yetkisi verilemez). Kapsam YAPILANDIRILABILIR.
        var scope = await PermissionScopeHandler.LoadScopeAsync(session);
        var notAssignable = command.DirectPermissions
            .Where(p => !AssignablePermissionScope.CanAssign(scope, account.Roles, p))
            .ToList();
        if (notAssignable.Count > 0)
            throw new DomainException(SecurityErrors.PermissionNotAssignableToRole(
                string.Join(", ", account.Roles), string.Join(", ", notAssignable)));

        var attributes = new Dictionary<string, string>
        {
            ["direct_permissions"] = string.Join(",", command.DirectPermissions)
        };
        var kcResult = await keycloak.SetUserAttributesAsync(account.KeycloakUserId, attributes);
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.DirectPermissions = command.DirectPermissions;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserPermissionsChanged(account.Id, account.KeycloakUserId, command.DirectPermissions);
    }
}

public static class ToggleUserStatusHandler
{
    public static async Task<object> Handle(
        ToggleUserStatus command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var kcResult = await keycloak.SetUserEnabledAsync(account.KeycloakUserId, command.Enable);
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.IsEnabled = command.Enable;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return command.Enable
            ? new UserActivated(account.Id, account.KeycloakUserId)
            : (object)new UserDeactivated(account.Id, account.KeycloakUserId, command.Reason ?? "");
    }
}

public static class DeleteUserHandler
{
    public static async Task<UserDeleted> Handle(
        DeleteUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var kcResult = await keycloak.DeleteUserAsync(account.KeycloakUserId);
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        session.Delete(account);

        return new UserDeleted(account.Id, account.KeycloakUserId);
    }
}

/// <summary>Senkronizasyon sonucu — toplam/yeni/güncellenen sayıları.</summary>
public sealed record SyncUsersResult(int Total, int Created, int Updated);

public static class SyncUsersFromKeycloakHandler
{
    public static async Task<SyncUsersResult> Handle(
        SyncUsersFromKeycloak command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var kcResult = await keycloak.GetUsersAsync();
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        var existing = await session.Query<UserAccount>().ToListAsync();
        var byKeycloakId = existing
            .GroupBy(u => u.KeycloakUserId)
            .ToDictionary(g => g.Key, g => g.First());

        int created = 0, updated = 0;
        foreach (var ku in kcResult.Value)
        {
            if (byKeycloakId.TryGetValue(ku.Id, out var account))
            {
                // Keycloak kaynak — temel alanları + rolleri tazele (lokal kurum/işletme bağı korunur, KC'de varsa güncellenir)
                account.Username = ku.Username;
                account.Email = ku.Email;
                account.FirstName = ku.FirstName;
                account.LastName = ku.LastName;
                account.IsEnabled = ku.Enabled;
                account.Roles = ku.Roles;
                if (ku.InstitutionId.HasValue) account.InstitutionId = ku.InstitutionId;
                if (ku.BusinessId.HasValue) account.BusinessId = ku.BusinessId;
                // Branş yalnız Keycloak'ta VARSA tazelenir; yoksa lokal kayıt korunur.
                // Boş öznitelik "branşı sil" anlamına gelmez — sync uydurmaz da, silmez de (#126).
                if (ku.BranchCodes.Count > 0) account.BranchCodes = ku.BranchCodes;
                account.UpdatedAt = DateTime.UtcNow;
                session.Store(account);
                updated++;
            }
            else
            {
                session.Store(new UserAccount
                {
                    Id = Guid.NewGuid(),
                    KeycloakUserId = ku.Id,
                    Username = ku.Username,
                    Email = ku.Email,
                    FirstName = ku.FirstName,
                    LastName = ku.LastName,
                    IsEnabled = ku.Enabled,
                    Roles = ku.Roles,
                    InstitutionId = ku.InstitutionId,
                    BusinessId = ku.BusinessId,
                    BranchCodes = ku.BranchCodes
                });
                created++;
            }
        }

        return new SyncUsersResult(kcResult.Value.Count, created, updated);
    }
}
