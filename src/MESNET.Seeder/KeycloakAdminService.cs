using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MESNET.Seeder;

/// <summary>
/// Keycloak Admin REST API ile mesnet realm kullanıcılarını günceller.
/// </summary>
public sealed class KeycloakAdminService
{
    private readonly HttpClient _http;
    private readonly string _keycloakBaseUrl; // örn. http://localhost:8080
    private string? _adminToken;
    private DateTime _expiresAt;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public KeycloakAdminService(HttpClient http, string keycloakBaseUrl)
    {
        _http = http;
        _keycloakBaseUrl = keycloakBaseUrl.TrimEnd('/');
    }

    private async Task<string> GetAdminTokenAsync()
    {
        if (_adminToken is not null && DateTime.UtcNow < _expiresAt.AddSeconds(-30))
            return _adminToken;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = "admin",
            ["password"] = "admin"
        };

        var response = await _http.PostAsync(
            $"{_keycloakBaseUrl}/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(form));

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOpts);
        _adminToken = tokenResponse!.AccessToken;
        _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        return _adminToken;
    }

    /// <summary>
    /// mesnet realm'daki tüm kullanıcıları döndürür.
    /// </summary>
    public async Task<List<KeycloakUser>> GetRealmUsersAsync()
    {
        var token = await GetAdminTokenAsync();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var url = $"{_keycloakBaseUrl}/admin/realms/mesnet/users?max=100";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<KeycloakUser>>(JsonOpts) ?? [];
    }

    /// <summary>
    /// mesnet realm'daki tüm kullanıcıların institution_id attribute'unu günceller.
    /// </summary>
    public async Task UpdateAllUsersInstitutionIdAsync(Guid institutionId)
    {
        var token = await GetAdminTokenAsync();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // mesnet realm kullanıcılarını listele
        var usersUrl = $"{_keycloakBaseUrl}/admin/realms/mesnet/users?max=100";
        var usersResponse = await _http.GetAsync(usersUrl);
        usersResponse.EnsureSuccessStatusCode();

        var users = await usersResponse.Content.ReadFromJsonAsync<List<KeycloakUser>>(JsonOpts);
        if (users is null || users.Count == 0) return;

        foreach (var user in users)
        {
            // Mevcut attribute'ları koru, institution_id'yi güncelle
            var attrs = user.Attributes ?? new Dictionary<string, List<string>>();
            attrs["institution_id"] = [institutionId.ToString()];

            var updateBody = new { attributes = attrs };
            var updateUrl = $"{_keycloakBaseUrl}/admin/realms/mesnet/users/{user.Id}";
            var updateResponse = await _http.PutAsJsonAsync(updateUrl, updateBody);
            updateResponse.EnsureSuccessStatusCode();
        }
    }

    public sealed class KeycloakUser
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = "";

        [JsonPropertyName("username")]
        public string Username { get; init; } = "";

        [JsonPropertyName("firstName")]
        public string FirstName { get; init; } = "";

        [JsonPropertyName("lastName")]
        public string LastName { get; init; } = "";

        [JsonPropertyName("attributes")]
        public Dictionary<string, List<string>>? Attributes { get; init; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
