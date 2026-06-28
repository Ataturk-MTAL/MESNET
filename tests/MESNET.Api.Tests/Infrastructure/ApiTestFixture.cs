using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MESNET.Api.Tests.Infrastructure;

/// <summary>
/// Çalışan Aspire API'sine black-box test bağlantısı.
/// Keycloak'tan bir kez token alır, Bearer'lı bir HttpClient sağlar.
/// Tüm bağlantı/kimlik değerleri env ile override edilebilir (dev varsayılanları yereldeki
/// Aspire kurulumuyla uyumlu; sır git'e gömülü değil — istenirse env'den verilir).
/// </summary>
public sealed class ApiTestFixture : IAsyncLifetime
{
    private static string ApiBaseUrl =>
        Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5270";

    private static string TokenUrl =>
        Environment.GetEnvironmentVariable("KEYCLOAK_TOKEN_URL")
        ?? "http://localhost:8080/realms/mesnet/protocol/openid-connect/token";

    private static string ClientId => Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_ID") ?? "mesnet-api";
    private static string ClientSecret => Environment.GetEnvironmentVariable("KEYCLOAK_CLIENT_SECRET") ?? "dev-secret";
    private static string Username => Environment.GetEnvironmentVariable("API_TEST_USERNAME") ?? "admin";
    private static string Password => Environment.GetEnvironmentVariable("API_TEST_PASSWORD") ?? "admin";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>Bearer token'lı authed client (admin kullanıcı).</summary>
    public HttpClient Client { get; private set; } = default!;

    /// <summary>Token'sız client — auth gerektiren endpoint'lerin 401 döndüğünü doğrulamak için.</summary>
    public HttpClient Anonymous { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        Anonymous = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
        // Authed client, API'nin aralıklı JWKS-warmup 401'lerine (IDX10500) karşı retry'li handler kullanır.
        // Geçerli token + 401 = anahtar cache race'i; istek tekrarlanınca (kısa backoff) geçer.
        Client = new HttpClient(new RetryOn401Handler { InnerHandler = new HttpClientHandler() })
        {
            BaseAddress = new Uri(ApiBaseUrl),
        };
        var token = await FetchTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        Anonymous?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<string> FetchTokenAsync()
    {
        using var http = new HttpClient();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["username"] = Username,
            ["password"] = Password,
        });
        var res = await http.PostAsync(TokenUrl, form);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Token alınamadı ({(int)res.StatusCode}). Aspire + Keycloak çalışıyor mu? Yanıt: {body}");
        }

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    /// <summary>API response zarfı: { code, type, data, ... } — data'yı JsonElement olarak döndürür.</summary>
    public static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return json.TryGetProperty("data", out var data) ? data : json;
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiTestFixture>;

/// <summary>
/// API'nin aralıklı JWKS-warmup kaynaklı 401'lerini (IDX10500 "No security keys") soğurur:
/// geçerli token'a rağmen 401 gelirse isteği klonlayıp kısa backoff'la birkaç kez tekrarlar.
/// Auth gerçekten gerekli/başarısızsa (kalıcı 401) son denemenin sonucu döner.
/// </summary>
public sealed class RetryOn401Handler : DelegatingHandler
{
    private const int MaxAttempts = 5;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // İçeriği buffer'la — istek tekrar gönderilebilsin
        byte[]? body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = request.Content?.Headers.ContentType;

        HttpResponseMessage response = default!;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var clone = Clone(request, body, contentType);
            response = await base.SendAsync(clone, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized || attempt == MaxAttempts)
                return response;
            response.Dispose();
            await Task.Delay(250 * attempt, cancellationToken); // 250,500,750,1000ms — JWKS warmup
        }
        return response;
    }

    private static HttpRequestMessage Clone(
        HttpRequestMessage req, byte[]? body, System.Net.Http.Headers.MediaTypeHeaderValue? contentType)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);
        foreach (var header in req.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            if (contentType is not null) clone.Content.Headers.ContentType = contentType;
        }
        return clone;
    }
}
