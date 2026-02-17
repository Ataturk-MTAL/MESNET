using System.Net.Http.Json;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using MESNET.Common.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MESNET.Security.Application.Services;

public sealed class KeycloakAdminService : IKeycloakAdminService
{
    private readonly IKeycloakUserClient _userClient;
    private readonly HttpClient _httpClient;
    private readonly string _realm;
    private readonly string _authServerUrl;
    private readonly ILogger<KeycloakAdminService> _logger;

    public KeycloakAdminService(
        IKeycloakUserClient userClient,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakAdminService> logger)
    {
        _userClient = userClient;
        _httpClient = httpClient;
        _realm = configuration["Keycloak:realm"] ?? "mesnet";
        _authServerUrl = (configuration["Keycloak:auth-server-url"] ?? "http://localhost:8080/").TrimEnd('/');
        _logger = logger;
    }

    public async Task<Result<string>> CreateUserAsync(
        string username, string email, string firstName, string lastName,
        string? temporaryPassword, CancellationToken ct = default)
    {
        try
        {
            var user = new UserRepresentation
            {
                Username = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Enabled = true,
                EmailVerified = true
            };

            if (!string.IsNullOrEmpty(temporaryPassword))
            {
                user.Credentials =
                [
                    new CredentialRepresentation
                    {
                        Type = "password",
                        Value = temporaryPassword,
                        Temporary = true
                    }
                ];
            }

            await _userClient.CreateUserAsync(_realm, user, ct);

            // Oluşturulan kullanıcının ID'sini al
            var users = await _userClient.GetUsersAsync(_realm,
                new GetUsersRequestParameters { Username = username }, ct);

            var createdUser = users?.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

            if (createdUser?.Id is null)
                return Result<string>.Failure(Errors.SecurityErrors.KeycloakOperationFailed(
                    "Kullanıcı oluşturuldu fakat ID alınamadı."));

            return Result<string>.Success(createdUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak kullanıcı oluşturma hatası: {Username}", username);
            return Result<string>.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    public async Task<Result> UpdateUserAsync(
        string keycloakUserId, string email, string firstName, string lastName,
        CancellationToken ct = default)
    {
        try
        {
            var user = new UserRepresentation
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };

            await _userClient.UpdateUserAsync(_realm, keycloakUserId, user, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak kullanıcı güncelleme hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    public async Task<Result> SetUserEnabledAsync(
        string keycloakUserId, bool enabled, CancellationToken ct = default)
    {
        try
        {
            var user = new UserRepresentation { Enabled = enabled };
            await _userClient.UpdateUserAsync(_realm, keycloakUserId, user, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak kullanıcı enable/disable hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    public async Task<Result> DeleteUserAsync(string keycloakUserId, CancellationToken ct = default)
    {
        try
        {
            await _userClient.DeleteUserAsync(_realm, keycloakUserId, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak kullanıcı silme hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    public async Task<Result> AssignRealmRolesAsync(
        string keycloakUserId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        try
        {
            // Realm'deki tüm rolleri REST API ile al
            var rolesResponse = await _httpClient.GetAsync(
                $"{_authServerUrl}/admin/realms/{_realm}/roles", ct);
            rolesResponse.EnsureSuccessStatusCode();

            var allRoles = await rolesResponse.Content
                .ReadFromJsonAsync<List<RoleRepresentation>>(ct) ?? [];

            var rolesToAssign = roles
                .Select(roleName => allRoles.FirstOrDefault(r =>
                    string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase)))
                .Where(r => r is not null)
                .Cast<RoleRepresentation>()
                .ToList();

            if (rolesToAssign.Count == 0)
                return Result.Success();

            var assignResponse = await _httpClient.PostAsJsonAsync(
                $"{_authServerUrl}/admin/realms/{_realm}/users/{keycloakUserId}/role-mappings/realm",
                rolesToAssign, ct);
            assignResponse.EnsureSuccessStatusCode();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak rol atama hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    public async Task<Result> RemoveRealmRolesAsync(
        string keycloakUserId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        try
        {
            var rolesResponse = await _httpClient.GetAsync(
                $"{_authServerUrl}/admin/realms/{_realm}/roles", ct);
            rolesResponse.EnsureSuccessStatusCode();

            var allRoles = await rolesResponse.Content
                .ReadFromJsonAsync<List<RoleRepresentation>>(ct) ?? [];

            var rolesToRemove = roles
                .Select(roleName => allRoles.FirstOrDefault(r =>
                    string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase)))
                .Where(r => r is not null)
                .Cast<RoleRepresentation>()
                .ToList();

            if (rolesToRemove.Count == 0)
                return Result.Success();

            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{_authServerUrl}/admin/realms/{_realm}/users/{keycloakUserId}/role-mappings/realm")
            {
                Content = JsonContent.Create(rolesToRemove)
            };

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak rol silme hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    public async Task<Result> SetUserAttributesAsync(
        string keycloakUserId, Dictionary<string, string> attributes, CancellationToken ct = default)
    {
        try
        {
            // Mevcut kullanıcıyı al
            var existingUser = await _userClient.GetUserAsync(_realm, keycloakUserId);
            if (existingUser is null)
                return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed("Kullanıcı bulunamadı."));

            // Attributes güncelle
            existingUser.Attributes ??= new Dictionary<string, ICollection<string>>();
            foreach (var (key, value) in attributes)
            {
                existingUser.Attributes[key] = new List<string> { value };
            }

            await _userClient.UpdateUserAsync(_realm, keycloakUserId, existingUser, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak attribute güncelleme hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }
}
