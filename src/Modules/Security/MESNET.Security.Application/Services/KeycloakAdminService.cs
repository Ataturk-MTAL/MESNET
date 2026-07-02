using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Keycloak.AuthServices.Sdk.Admin.Models;
using MESNET.Common.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MESNET.Security.Application.Services;

public sealed class KeycloakAdminService : IKeycloakAdminService
{
    private readonly HttpClient _httpClient;
    private readonly string _realm;
    private readonly string _authServerUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<KeycloakAdminService> _logger;

    // Admin API token cache'i — servis Scoped (request başına yeni instance); bir istek
    // içindeki çok sayıda Admin API çağrısı için tek token yeter.
    private string? _cachedAdminToken;
    private DateTime _adminTokenExpiry = DateTime.MinValue;

    public KeycloakAdminService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakAdminService> logger)
    {
        _httpClient = httpClient;
        _realm = configuration["Keycloak:realm"] ?? "mesnet";
        _authServerUrl = (configuration["Keycloak:auth-server-url"] ?? "http://localhost:8080/").TrimEnd('/');
        _clientId = configuration["Keycloak:resource"] ?? "mesnet-api";
        _clientSecret = configuration["Keycloak:credentials:secret"] ?? string.Empty;
        _logger = logger;
    }

    // ── Admin API auth ────────────────────────────────────────────────────────────────
    // mesnet-api service-account'ı için client_credentials access token alır.
    // Keycloak.AuthServices SDK admin client'ı (IKeycloakUserClient) ve named HttpClient
    // çalışan bir Bearer EKLEMEDIĞINDEN Admin API'ye 401 dönüyordu; bu yüzden token'ı elle
    // alıp TÜM Admin API çağrılarında Bearer olarak kullanıyoruz. Service-account'a
    // realm-management rolleri atanmıştır (bkz. mesnet-realm.json).
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

    // Admin API'ye yetkili (Bearer) istek gönderir. 'path' realm-altı yoldur, ör. "/users".
    private async Task<HttpResponseMessage> SendAdminAsync(
        HttpMethod method, string path, object? jsonBody, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);
        var req = new HttpRequestMessage(method, $"{_authServerUrl}/admin/realms/{_realm}{path}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (jsonBody is not null)
            req.Content = JsonContent.Create(jsonBody);
        return await _httpClient.SendAsync(req, ct);
    }

    // ── Kullanıcı CRUD ────────────────────────────────────────────────────────────────
    public async Task<Result<string>> CreateUserAsync(
        string username, string email, string firstName, string lastName,
        string? temporaryPassword, CancellationToken ct = default)
    {
        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["username"] = username,
                ["email"] = email,
                ["firstName"] = firstName,
                ["lastName"] = lastName,
                ["enabled"] = true,
                ["emailVerified"] = true,
            };
            if (!string.IsNullOrEmpty(temporaryPassword))
            {
                payload["credentials"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["type"] = "password",
                        ["value"] = temporaryPassword,
                        ["temporary"] = true,
                    },
                };
            }

            using var createResp = await SendAdminAsync(HttpMethod.Post, "/users", payload, ct);
            createResp.EnsureSuccessStatusCode();

            // Keycloak 201 + Location: .../users/{id}
            var id = createResp.Headers.Location?.Segments.LastOrDefault()?.Trim('/');
            if (string.IsNullOrEmpty(id))
            {
                using var listResp = await SendAdminAsync(
                    HttpMethod.Get, $"/users?username={Uri.EscapeDataString(username)}&exact=true", null, ct);
                listResp.EnsureSuccessStatusCode();
                var found = await listResp.Content.ReadFromJsonAsync<List<JsonElement>>(ct) ?? [];
                if (found.Count > 0 && found[0].TryGetProperty("id", out var idEl))
                    id = idEl.GetString();
            }

            if (string.IsNullOrEmpty(id))
                return Result<string>.Failure(Errors.SecurityErrors.KeycloakOperationFailed(
                    "Kullanıcı oluşturuldu fakat ID alınamadı."));

            return Result<string>.Success(id);
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
            var payload = new Dictionary<string, object?>
            {
                ["email"] = email,
                ["firstName"] = firstName,
                ["lastName"] = lastName,
            };
            using var resp = await SendAdminAsync(HttpMethod.Put, $"/users/{keycloakUserId}", payload, ct);
            resp.EnsureSuccessStatusCode();
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
            var payload = new Dictionary<string, object?> { ["enabled"] = enabled };
            using var resp = await SendAdminAsync(HttpMethod.Put, $"/users/{keycloakUserId}", payload, ct);
            resp.EnsureSuccessStatusCode();
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
            using var resp = await SendAdminAsync(HttpMethod.Delete, $"/users/{keycloakUserId}", null, ct);
            resp.EnsureSuccessStatusCode();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak kullanıcı silme hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    // ── Rol atama ─────────────────────────────────────────────────────────────────────
    public async Task<Result> AssignRealmRolesAsync(
        string keycloakUserId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        try
        {
            var rolesToAssign = await ResolveRealmRolesAsync(roles, ct);
            if (rolesToAssign.Count == 0)
                return Result.Success();

            using var resp = await SendAdminAsync(
                HttpMethod.Post, $"/users/{keycloakUserId}/role-mappings/realm", rolesToAssign, ct);
            resp.EnsureSuccessStatusCode();
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
            var rolesToRemove = await ResolveRealmRolesAsync(roles, ct);
            if (rolesToRemove.Count == 0)
                return Result.Success();

            using var resp = await SendAdminAsync(
                HttpMethod.Delete, $"/users/{keycloakUserId}/role-mappings/realm", rolesToRemove, ct);
            resp.EnsureSuccessStatusCode();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak rol silme hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    // Verilen rol adlarını realm'deki RoleRepresentation'lara (id+name gerekli) çevirir.
    private async Task<List<RoleRepresentation>> ResolveRealmRolesAsync(
        IEnumerable<string> roles, CancellationToken ct)
    {
        using var resp = await SendAdminAsync(HttpMethod.Get, "/roles", null, ct);
        resp.EnsureSuccessStatusCode();
        var allRoles = await resp.Content.ReadFromJsonAsync<List<RoleRepresentation>>(ct) ?? [];
        return roles
            .Select(roleName => allRoles.FirstOrDefault(r =>
                string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase)))
            .Where(r => r is not null)
            .Cast<RoleRepresentation>()
            .ToList();
    }

    // ── Attribute ─────────────────────────────────────────────────────────────────────
    public async Task<Result> SetUserAttributesAsync(
        string keycloakUserId, Dictionary<string, string> attributes, CancellationToken ct = default)
    {
        try
        {
            using var getResp = await SendAdminAsync(HttpMethod.Get, $"/users/{keycloakUserId}", null, ct);
            if (getResp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed("Kullanıcı bulunamadı."));
            getResp.EnsureSuccessStatusCode();
            var existing = await getResp.Content.ReadFromJsonAsync<JsonElement>(ct);

            // Mevcut attribute'ları koru, yenileri üzerine yaz
            var merged = new Dictionary<string, List<string>>();
            if (existing.TryGetProperty("attributes", out var existingAttrs)
                && existingAttrs.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in existingAttrs.EnumerateObject())
                {
                    merged[prop.Name] = prop.Value.ValueKind == JsonValueKind.Array
                        ? prop.Value.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList()
                        : [];
                }
            }
            foreach (var (key, value) in attributes)
                merged[key] = [value];

            var payload = new Dictionary<string, object?> { ["attributes"] = merged };
            using var putResp = await SendAdminAsync(HttpMethod.Put, $"/users/{keycloakUserId}", payload, ct);
            putResp.EnsureSuccessStatusCode();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak attribute güncelleme hatası: {UserId}", keycloakUserId);
            return Result.Failure(Errors.SecurityErrors.KeycloakOperationFailed(ex.Message));
        }
    }

    // ── Listeleme ─────────────────────────────────────────────────────────────────────
    public async Task<Result<List<KeycloakUserInfo>>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            using var listResp = await SendAdminAsync(
                HttpMethod.Get, "/users?max=1000&briefRepresentation=false", null, ct);
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

                // Kullanıcının realm rolleri
                var roles = new List<string>();
                using var rolesResp = await SendAdminAsync(
                    HttpMethod.Get, $"/users/{id}/role-mappings/realm", null, ct);
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
