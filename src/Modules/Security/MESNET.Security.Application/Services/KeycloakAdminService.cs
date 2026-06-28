using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<KeycloakAdminService> _logger;

    // Admin API token cache'i (scope: request başına yeni instance — bir sync içindeki
    // çok sayıda role-mappings çağrısı için tek token yeter).
    private string? _cachedAdminToken;
    private DateTime _adminTokenExpiry = DateTime.MinValue;

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
        _clientId = configuration["Keycloak:resource"] ?? "mesnet-api";
        _clientSecret = configuration["Keycloak:credentials:secret"] ?? string.Empty;
        _logger = logger;
    }

    // mesnet-api service-account'ı için client_credentials access token alır.
    // SDK admin client'ının (IKeycloakUserClient) token handler'ı 401 verdiği için Admin API
    // çağrılarında bu token Bearer olarak elle kullanılır. Service-account'a realm-management
    // rolleri atanmıştır (bkz. mesnet-realm.json).
    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        if (_cachedAdminToken is not null && DateTime.UtcNow < _adminTokenExpiry)
            return _cachedAdminToken;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["grant_type"] = "client_credentials",
        });
        using var resp = await _httpClient.PostAsync(
            $"{_authServerUrl}/realms/{_realm}/protocol/openid-connect/token", content, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        _cachedAdminToken = json.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Keycloak token cevabında access_token yok.");
        var expiresIn = json.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 60;
        _adminTokenExpiry = DateTime.UtcNow.AddSeconds(Math.Max(expiresIn - 15, 15));
        return _cachedAdminToken;
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

    public async Task<Result<List<KeycloakUserInfo>>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            var token = await GetAdminTokenAsync(ct);

            // Kullanıcı listesi — SDK client'ı (IKeycloakUserClient) auth handler'ı 401 verdiği
            // için Admin API'ye client_credentials token'ı ile raw HTTP gidilir.
            using var listReq = new HttpRequestMessage(HttpMethod.Get,
                $"{_authServerUrl}/admin/realms/{_realm}/users?max=1000&briefRepresentation=false");
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var listResp = await _httpClient.SendAsync(listReq, ct);
            listResp.EnsureSuccessStatusCode();
            var users = await listResp.Content.ReadFromJsonAsync<List<JsonElement>>(ct) ?? [];

            var result = new List<KeycloakUserInfo>();
            foreach (var u in users)
            {
                var id = u.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var username = u.TryGetProperty("username", out var unEl) ? unEl.GetString() ?? string.Empty : string.Empty;

                // Service-account kullanıcıları (ör. service-account-mesnet-api) listeye dahil edilmez
                if (string.IsNullOrEmpty(id) ||
                    username.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Kullanıcının realm rolleri (aynı token ile)
                var roles = new List<string>();
                using var rolesReq = new HttpRequestMessage(HttpMethod.Get,
                    $"{_authServerUrl}/admin/realms/{_realm}/users/{id}/role-mappings/realm");
                rolesReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var rolesResp = await _httpClient.SendAsync(rolesReq, ct);
                if (rolesResp.IsSuccessStatusCode)
                {
                    var roleReps = await rolesResp.Content
                        .ReadFromJsonAsync<List<RoleRepresentation>>(ct) ?? [];
                    roles = roleReps
                        .Where(r => !string.IsNullOrEmpty(r.Name))
                        .Select(r => r.Name!)
                        .ToList();
                }

                result.Add(new KeycloakUserInfo(
                    id,
                    username,
                    u.TryGetProperty("email", out var emEl) ? emEl.GetString() ?? string.Empty : string.Empty,
                    u.TryGetProperty("firstName", out var fnEl) ? fnEl.GetString() ?? string.Empty : string.Empty,
                    u.TryGetProperty("lastName", out var lnEl) ? lnEl.GetString() ?? string.Empty : string.Empty,
                    !u.TryGetProperty("enabled", out var enEl) || enEl.GetBoolean(),
                    roles,
                    ParseGuidJsonAttr(u, "institution_id"),
                    ParseGuidJsonAttr(u, "business_id")));
            }

            return Result<List<KeycloakUserInfo>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak kullanıcı listesi alma hatası");
            return Result<List<KeycloakUserInfo>>.Failure(
                Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    // Keycloak UserRepresentation.attributes → { "institution_id": ["<guid>"] } biçimindedir.
    private static Guid? ParseGuidJsonAttr(JsonElement user, string key)
    {
        if (user.TryGetProperty("attributes", out var attrs)
            && attrs.ValueKind == JsonValueKind.Object
            && attrs.TryGetProperty(key, out var arr)
            && arr.ValueKind == JsonValueKind.Array
            && arr.GetArrayLength() > 0
            && arr[0].GetString() is { } raw
            && Guid.TryParse(raw, out var guid))
            return guid;
        return null;
    }
}
