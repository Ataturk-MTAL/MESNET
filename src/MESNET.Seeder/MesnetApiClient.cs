using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MESNET.Seeder;

public sealed class MesnetApiClient
{
    private readonly HttpClient _http;
    private readonly KeycloakTokenService _tokenService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MesnetApiClient(HttpClient http, KeycloakTokenService tokenService)
    {
        _http = http;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Başarısız (2xx olmayan) çağrı sayısı. Program.cs bunu özetler ve sıfırdan farklıysa
    /// çıkış kodunu sıfırdan farklı yapar — sessizce yutulan hata kalmasın (#80).
    /// </summary>
    public int FailureCount { get; private set; }

    /// <summary>
    /// Liste endpoint'ini okur ve hem düz dizi hem de PagedResult (<c>{ items: [...] }</c>)
    /// gövdesini destekler. Sayfalama projeye sonradan geldiği için bazı endpoint'ler dizi,
    /// bazıları PagedResult döndürüyor; çağıran tarafın bunu bilmesi gerekmesin (#80).
    /// </summary>
    public async Task<IReadOnlyList<JsonElement>> GetListAsync(string url)
        => ToItems(await GetAsync(url));

    public static IReadOnlyList<JsonElement> ToItems(JsonElement? data)
    {
        if (data is not { } el)
            return [];

        if (el.ValueKind == JsonValueKind.Array)
            return [.. el.EnumerateArray()];

        if (el.ValueKind == JsonValueKind.Object
            && el.TryGetProperty("items", out var items)
            && items.ValueKind == JsonValueKind.Array)
            return [.. items.EnumerateArray()];

        return [];
    }

    public async Task<JsonElement?> PostAsync(string url, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        await AddAuthAsync(request);
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            FailureCount++;
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  ✗ POST {url} → {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"    {errorBody[..Math.Min(errorBody.Length, 300)]}");
            return null;
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope>(JsonOptions);
        return envelope?.Data;
    }

    public async Task<JsonElement?> PutAsync(string url, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        await AddAuthAsync(request);
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            FailureCount++;
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  ✗ PUT {url} → {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"    {errorBody[..Math.Min(errorBody.Length, 300)]}");
            return null;
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope>(JsonOptions);
        return envelope?.Data;
    }

    public async Task<JsonElement?> PatchAsync(string url, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        await AddAuthAsync(request);
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            FailureCount++;
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  ✗ PATCH {url} → {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"    {errorBody[..Math.Min(errorBody.Length, 300)]}");
            return null;
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope>(JsonOptions);
        return envelope?.Data;
    }

    public async Task<JsonElement?> GetAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        await AddAuthAsync(request);
        var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            FailureCount++;
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  ✗ GET {url} → {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"    {errorBody[..Math.Min(errorBody.Length, 300)]}");
            return null;
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope>(JsonOptions);
        return envelope?.Data;
    }

    private async Task AddAuthAsync(HttpRequestMessage request)
    {
        var token = await _tokenService.GetTokenAsync();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record ApiEnvelope
    {
        public int Code { get; init; }
        public string? Type { get; init; }
        public string? Message { get; init; }
        public JsonElement? Data { get; init; }
    }
}
