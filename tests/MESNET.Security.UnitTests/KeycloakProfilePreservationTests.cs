using System.Net;
using System.Text;
using System.Text.Json;
using MESNET.Security.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Keycloak öznitelik yazımı, kullanıcının ad/soyad/e-postasını SİLMEMELİDİR (#190).
///
/// <para><b>Yaşanan hata:</b> öznitelik güncellemesi gövdeye yalnız <c>attributes</c> koyuyordu.
/// Keycloak'ın <i>declarative user profile</i> sağlayıcısı, PUT gövdesinde <c>attributes</c>
/// varsa onu <b>managed profil alanlarının tamamı</b> sayar; gövdede geçmeyen
/// <c>firstName</c>/<c>lastName</c> silinir, <c>email</c> yalnız üst seviye alandan okunduğu
/// için o da silinir.</para>
///
/// <para><b>Neden fark edilmedi:</b> istek <b>204 No Content</b> döner. Hata yok, log yok,
/// <c>EnsureSuccessStatusCode()</c> memnun. Canlı realm'de 7 kullanıcının 7'sinin adı, soyadı
/// ve e-postası boştu; personel kayıtları da bu yüzden boş adla açılmıştı. Bu yüzden test
/// durum koduna değil <b>gövdenin içeriğine</b> bakar.</para>
///
/// <para>Kilitlenen değişmez: <c>attributes</c> taşıyan her PUT, kullanıcının mevcut profil
/// alanlarını da <b>aynı değerlerle</b> taşımalıdır.</para>
/// </summary>
public sealed class KeycloakProfilePreservationTests
{
    private const string UserId = "c8615312-8412-47d6-9d45-3e7d7b865c44";
    private const string FirstName = "Ahmet";
    private const string LastName = "Öğretmen";
    private const string Email = "teacher1@mesnet.local";

    // ── Öznitelik yazımı profil alanlarını korur ────────────────────────────────────────

    [Fact]
    public async Task Cok_degerli_oznitelik_yazimi_ad_soyad_epostayi_silmez()
    {
        var (service, handler) = CreateService();

        var result = await service.SetUserAttributeValuesAsync(UserId, "branch_codes", ["EET"]);

        result.IsSuccess.ShouldBeTrue();
        var body = handler.LastPutBody.ShouldNotBeNull();
        ProfileAlanlariniDogrula(body);
    }

    [Fact]
    public async Task Tek_degerli_oznitelik_yazimi_ad_soyad_epostayi_silmez()
    {
        var (service, handler) = CreateService();

        var result = await service.SetUserAttributesAsync(
            UserId, new Dictionary<string, string> { ["business_id"] = Guid.NewGuid().ToString() });

        result.IsSuccess.ShouldBeTrue();
        var body = handler.LastPutBody.ShouldNotBeNull();
        ProfileAlanlariniDogrula(body);
    }

    /// <summary>
    /// Boş liste özniteliği siler (#126) — ama yalnız o özniteliği. Silme yolunda da profil
    /// alanları gövdede kalmalıdır; aksi hâlde "branşı kaldır" işlemi adı da siler.
    /// </summary>
    [Fact]
    public async Task Oznitelik_silmede_de_profil_alanlari_korunur()
    {
        var (service, handler) = CreateService();

        var result = await service.SetUserAttributeValuesAsync(UserId, "branch_codes", []);

        result.IsSuccess.ShouldBeTrue();
        var body = handler.LastPutBody.ShouldNotBeNull();
        ProfileAlanlariniDogrula(body);

        var attributes = body.GetProperty("attributes");
        attributes.TryGetProperty("branch_codes", out _)
            .ShouldBeFalse("Boş liste özniteliği kaldırmalı.");
    }

    // ── Öznitelik birleştirme davranışı ────────────────────────────────────────────────

    [Fact]
    public async Task Mevcut_oznitelikler_korunur_yenisi_eklenir()
    {
        var (service, handler) = CreateService();

        await service.SetUserAttributeValuesAsync(UserId, "branch_codes", ["EET", "MTT"]);

        var attributes = handler.LastPutBody.ShouldNotBeNull().GetProperty("attributes");

        // Var olan öznitelik silinmemeli — kapsam kararı buna bakıyor.
        attributes.GetProperty("institution_id")[0].GetString()
            .ShouldBe("ea0d411b-38a4-41c9-a653-e535ba3fa77c");

        attributes.GetProperty("branch_codes")
            .EnumerateArray().Select(v => v.GetString())
            .ShouldBe(["EET", "MTT"]);
    }

    // ── Yardımcılar ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gövde, kullanıcının GET'ten okunan profil alanlarını aynı değerlerle taşımalı.
    /// Alanın <b>yokluğu</b> da <b>null olması</b> da Keycloak'ta silinme demektir.
    /// </summary>
    private static void ProfileAlanlariniDogrula(JsonElement body)
    {
        Alan(body, "firstName").ShouldBe(FirstName);
        Alan(body, "lastName").ShouldBe(LastName);
        Alan(body, "email").ShouldBe(Email);
        Alan(body, "username").ShouldBe("teacher1");

        static string? Alan(JsonElement body, string name)
        {
            if (!body.TryGetProperty(name, out var value))
            {
                throw new Xunit.Sdk.XunitException(
                    $"PUT gövdesinde '{name}' yok. Keycloak, gövdede 'attributes' varken " +
                    $"geçmeyen profil alanını SİLER ve yine 204 döner. Kısmi gövde yerine " +
                    $"GET'ten dönen temsilin tamamını gönderin (#190).");
            }

            return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        }
    }

    private static (KeycloakAdminService Service, StubHandler Handler) CreateService()
    {
        var handler = new StubHandler();
        var service = new KeycloakAdminService(
            new HttpClient(handler),
            new StubConfiguration(new Dictionary<string, string?>
            {
                ["Keycloak:realm"] = "mesnet",
                ["Keycloak:auth-server-url"] = "http://localhost:8080/",
                ["Keycloak:resource"] = "mesnet-api",
                ["Keycloak:credentials:secret"] = "dev-secret",
            }),
            NullLogger<KeycloakAdminService>.Instance);

        return (service, handler);
    }

    /// <summary>
    /// Keycloak Admin API'sini taklit eder. GET, <b>gerçek Keycloak 26 temsilini</b> döndürür
    /// (salt-okunur alanlar dâhil); PUT gövdesi incelenmek üzere saklanır.
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private const string UserRepresentation = $$"""
        {
          "id": "{{UserId}}",
          "username": "teacher1",
          "firstName": "{{FirstName}}",
          "lastName": "{{LastName}}",
          "email": "{{Email}}",
          "emailVerified": true,
          "enabled": true,
          "createdTimestamp": 1782335899430,
          "totp": false,
          "disableableCredentialTypes": [],
          "requiredActions": [],
          "notBefore": 0,
          "access": { "manage": true },
          "attributes": { "institution_id": ["ea0d411b-38a4-41c9-a653-e535ba3fa77c"] }
        }
        """;

        public JsonElement? LastPutBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal))
                return Json("""{"access_token":"test-token","expires_in":300}""");

            if (path.EndsWith($"/users/{UserId}", StringComparison.Ordinal))
            {
                if (request.Method == HttpMethod.Get)
                    return Json(UserRepresentation);

                if (request.Method == HttpMethod.Put)
                {
                    var raw = await request.Content!.ReadAsStringAsync(cancellationToken);
                    LastPutBody = JsonSerializer.Deserialize<JsonElement>(raw);

                    // Gerçek Keycloak burada 204 döner — gövde eksik olsa BİLE. Sessiz veri
                    // kaybının kaynağı bu; test durum koduna değil gövdeye bakmalı.
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(string content) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };
    }

    /// <summary>Yalnız anahtar okuması yapan minimal <see cref="IConfiguration"/>.</summary>
    private sealed class StubConfiguration(Dictionary<string, string?> values) : IConfiguration
    {
        public string? this[string key]
        {
            get => values.GetValueOrDefault(key);
            set => values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() =>
            throw new NotSupportedException("Test yapılandırması yeniden yüklenmez.");

        public IConfigurationSection GetSection(string key) =>
            throw new NotSupportedException("Test yapılandırması bölüm döndürmez.");
    }
}
