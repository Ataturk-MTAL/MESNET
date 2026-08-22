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
    /// Gövdesinde <c>data</c> taşımayan 2xx yanıtın karşılığı — <b>başarı, veri yok</b> (#204).
    ///
    /// <para><b>Neden gerekli:</b> yazma uçlarının bir kısmı bilerek veri döndürmez;
    /// <c>ResponseBuilder.Success().AddMessage(...)</c> gövdeyi <c>"data": null</c> ile kapatır.
    /// System.Text.Json bunu <c>JsonElement?</c> alanında C# <c>null</c>'ına indirger ve o an
    /// "veri yok" ile "çağrı başarısız" ayırt edilemez hâle gelir.</para>
    ///
    /// <para>Ayrım kaybolunca iki yönlü yalan doğuyordu: alan aktivasyonu <b>başarılı olduğu
    /// hâlde</b> ✗ basılıyor ve <c>continue</c> ile dal yazımı atlanıyordu; asgari ücret ise
    /// <b>yazıldığı hâlde</b> hiç satır basmıyordu. Yanıt 2xx olduğu için
    /// <see cref="FailureCount"/> de artmıyor, çıkış kodu sıfır kalıyordu.</para>
    ///
    /// <para>Bundan sonra <c>null</c> dönüşü <b>yalnız</b> gerçek hatayı (2xx olmayan yanıt)
    /// gösterir. <see cref="GetAsync"/> bunun dışındadır: orada 404 → <c>null</c> "kayıt yok"
    /// demektir ve seeder'ın "zaten var mı" kontrolleri buna dayanır.</para>
    /// </summary>
    public static readonly JsonElement NoData = JsonDocument.Parse("null").RootElement;

    /// <summary>
    /// Liste endpoint'ini okur ve hem düz dizi hem de PagedResult (<c>{ items: [...] }</c>)
    /// gövdesini destekler. Sayfalama projeye sonradan geldiği için bazı endpoint'ler dizi,
    /// bazıları PagedResult döndürüyor; çağıran tarafın bunu bilmesi gerekmesin (#80).
    /// </summary>
    /// <summary>
    /// Token önbelleğini düşürür — kullanıcı öznitelikleri (ör. <c>institution_id</c>)
    /// değiştiğinde çağrılır, yoksa eski claim'lerle devam edilir.
    /// </summary>
    public void RefreshToken() => _tokenService.Invalidate();

    public async Task<IReadOnlyList<JsonElement>> GetListAsync(string url)
        => ToItems(await GetAsync(url));

    /// <summary>Sunucunun kabul ettiği en büyük sayfa boyutu (<c>PagedQuery.SafePageSize</c>).</summary>
    private const int MaxPageSize = 100;

    /// <summary>
    /// Sayfalı bir listenin <b>tamamını</b> gezerek döndürür.
    ///
    /// <para><b>Neden gerekli:</b> <c>PagedQuery.SafePageSize</c> istenen sayfa boyutunu
    /// <c>Math.Clamp(PageSize, 1, 100)</c> ile kırpar ve bunu <b>sessizce</b> yapar — istek
    /// yine 200 döner, gövde yalnız 100 kayıt taşır. Seeder'daki
    /// <c>?pageSize=200</c> / <c>?pageSize=500</c> çağrıları bu yüzden listenin geri kalanını
    /// hiç görmüyordu.</para>
    ///
    /// <para><b>Sonucu:</b> "zaten var mı" kontrolleri eksik veriyle karar veriyor ve kayıt her
    /// koşuda yeniden yaratılıyordu. Ölçülen hasar: 122 gerçek öğrenci için <b>774 öğrenci
    /// kaydı</b>, kimileri 24 kopya. Aynı örüntü personel kayıtlarında da yaşanmıştı (#190).</para>
    ///
    /// <para>Sorgu dizesi taşıyan yollar desteklenir; <c>page</c>/<c>pageSize</c> eklenir.</para>
    /// </summary>
    public async Task<IReadOnlyList<JsonElement>> GetAllPagedAsync(string url)
    {
        var separator = url.Contains('?') ? '&' : '?';
        var all = new List<JsonElement>();

        for (var page = 1; ; page++)
        {
            var items = ToItems(await GetAsync($"{url}{separator}page={page}&pageSize={MaxPageSize}"));
            all.AddRange(items);

            // Dolu bir sayfadan az geldiyse son sayfadayız. Sayfa hiç gelmediyse de biter.
            if (items.Count < MaxPageSize) break;
        }

        return all;
    }

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
        // Veri yoksa NoData: 2xx her zaman başarıdır, null yalnız gerçek hatayı gösterir (#204).
        return envelope?.Data ?? NoData;
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
        // Veri yoksa NoData: 2xx her zaman başarıdır, null yalnız gerçek hatayı gösterir (#204).
        return envelope?.Data ?? NoData;
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
        // Veri yoksa NoData: 2xx her zaman başarıdır, null yalnız gerçek hatayı gösterir (#204).
        return envelope?.Data ?? NoData;
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
