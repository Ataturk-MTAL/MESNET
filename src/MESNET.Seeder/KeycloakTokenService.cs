using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MESNET.Seeder;

public sealed class KeycloakTokenService
{
    private readonly HttpClient _http;
    private readonly SeederOptions _options;
    private string? _token;
    private DateTime _expiresAt;

    public KeycloakTokenService(HttpClient http, SeederOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<string> GetTokenAsync()
    {
        if (_token is not null && DateTime.UtcNow < _expiresAt.AddSeconds(-30))
            return _token;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = _options.Username ?? "admin",
            ["password"] = _options.Password ?? "admin"
        };

        var response = await _http.PostAsync(
            _options.KeycloakTokenUrl,
            new FormUrlEncodedContent(form));

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        _token = tokenResponse!.AccessToken;
        _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        return _token;
    }

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
