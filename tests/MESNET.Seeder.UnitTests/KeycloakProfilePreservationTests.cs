using System.Net;
using System.Text;
using System.Text.Json;
using MESNET.Seeder;
using Shouldly;
using Xunit;

namespace MESNET.Seeder.UnitTests;

/// <summary>
/// Seeder'ın <c>institution_id</c> yazımı, realm kullanıcılarının ad/soyad/e-postasını
/// SİLMEMELİDİR (#190).
///
/// <para><b>Yaşanan hasar:</b> <c>UpdateAllUsersInstitutionIdAsync</c> gövdeye yalnız
/// <c>attributes</c> koyuyordu. Keycloak'ın <i>declarative user profile</i> sağlayıcısı,
/// gövdede <c>attributes</c> varsa onu managed profil alanlarının tamamı sayar; gövdede
/// geçmeyen <c>firstName</c>/<c>lastName</c>/<c>email</c> silinir. Bu metot realm'daki
/// <b>TÜM kullanıcılar</b> üzerinde döndüğü için her seeder çalıştırması herkesin adını
/// siliyordu. Sonra aynı seeder o boş adı okuyup personel kaydı açıyordu — kurum sayfasındaki
/// 205 satırın 205'i boş adlıydı.</para>
///
/// <para><b>Neden sessizdi:</b> Keycloak eksik gövdeye de <b>204 No Content</b> döner.
/// <c>EnsureSuccessStatusCode()</c> memnun, log temiz. Bu yüzden test durum koduna değil
/// gönderilen gövdeye bakar.</para>
/// </summary>
public sealed class KeycloakProfilePreservationTests
{
    private static readonly Guid InstitutionId = Guid.Parse("ea0d411b-38a4-41c9-a653-e535ba3fa77c");

    [Fact]
    public async Task Institution_id_yazimi_ad_soyad_epostayi_silmez()
    {
        var handler = new StubHandler();
        var service = new KeycloakAdminService(new HttpClient(handler), "http://localhost:8080");

        await service.UpdateAllUsersInstitutionIdAsync(InstitutionId);

        handler.PutBodies.Count.ShouldBe(2, "Realm'daki her kullanıcı güncellenmeli.");

        var teacher = handler.PutBodies.Single(b => Alan(b, "username") == "teacher1");
        Alan(teacher, "firstName").ShouldBe("Ahmet");
        Alan(teacher, "lastName").ShouldBe("Öğretmen");
        Alan(teacher, "email").ShouldBe("teacher1@mesnet.local");
    }

    /// <summary>Adı zaten boş olan kullanıcı (ör. service-account) bu yolda sorun çıkarmamalı.</summary>
    [Fact]
    public async Task Profil_alani_olmayan_kullanici_da_guncellenir()
    {
        var handler = new StubHandler();
        var service = new KeycloakAdminService(new HttpClient(handler), "http://localhost:8080");

        await service.UpdateAllUsersInstitutionIdAsync(InstitutionId);

        var serviceAccount = handler.PutBodies.Single(
            b => Alan(b, "username") == "service-account-mesnet-api");

        serviceAccount.GetProperty("attributes").GetProperty("institution_id")[0]
            .GetString().ShouldBe(InstitutionId.ToString());
    }

    [Fact]
    public async Task Mevcut_oznitelikler_korunur_institution_id_uzerine_yazilir()
    {
        var handler = new StubHandler();
        var service = new KeycloakAdminService(new HttpClient(handler), "http://localhost:8080");

        await service.UpdateAllUsersInstitutionIdAsync(InstitutionId);

        var attributes = handler.PutBodies
            .Single(b => Alan(b, "username") == "teacher1")
            .GetProperty("attributes");

        // branch_codes kapsam kararının girdisidir; silinirse alan şefi yazma yetkisini kaybeder.
        attributes.GetProperty("branch_codes").EnumerateArray()
            .Select(v => v.GetString()).ShouldBe(["EET"]);

        attributes.GetProperty("institution_id")[0].GetString()
            .ShouldBe(InstitutionId.ToString());
    }

    /// <summary>Alanın yokluğu da null olması da Keycloak'ta "sil" demektir.</summary>
    private static string? Alan(JsonElement body, string name)
    {
        if (!body.TryGetProperty(name, out var value))
        {
            throw new Xunit.Sdk.XunitException(
                $"PUT gövdesinde '{name}' yok. Keycloak, gövdede 'attributes' varken geçmeyen " +
                $"profil alanını SİLER ve yine 204 döner. Kısmi gövde yerine GET'ten dönen " +
                $"temsilin tamamını gönderin (#190).");
        }

        return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
    }

    /// <summary>
    /// Keycloak Admin API'sini taklit eder. Kullanıcı listesi <b>gerçek Keycloak 26 temsilidir</b>
    /// (salt-okunur alanlar dâhil); PUT gövdeleri incelenmek üzere saklanır.
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private const string Users = """
        [
          {
            "id": "c8615312-8412-47d6-9d45-3e7d7b865c44",
            "username": "teacher1",
            "firstName": "Ahmet",
            "lastName": "Öğretmen",
            "email": "teacher1@mesnet.local",
            "emailVerified": true,
            "enabled": true,
            "createdTimestamp": 1782335899430,
            "totp": false,
            "disableableCredentialTypes": [],
            "requiredActions": [],
            "notBefore": 0,
            "access": { "manage": true },
            "attributes": { "branch_codes": ["EET"], "institution_id": ["00000000-0000-0000-0000-000000000000"] }
          },
          {
            "id": "1f4a6a0e-0000-4000-8000-000000000001",
            "username": "service-account-mesnet-api",
            "emailVerified": false,
            "enabled": true,
            "createdTimestamp": 1782335899000,
            "totp": false,
            "disableableCredentialTypes": [],
            "requiredActions": [],
            "notBefore": 0,
            "access": { "manage": true }
          }
        ]
        """;

        public List<JsonElement> PutBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal))
                return Json("""{"access_token":"test-token","expires_in":300}""");

            if (request.Method == HttpMethod.Get && path.EndsWith("/users", StringComparison.Ordinal))
            {
                // briefRepresentation=false açıkça istenmeli — kısa temsil profil alanlarını
                // atlarsa aşağıdaki tam-temsil PUT'u onları silmiş olurdu.
                request.RequestUri.Query.ShouldContain(
                    "briefRepresentation=false",
                    customMessage: "Kullanıcı listesi tam temsille istenmeli (#190).");

                return Json(Users);
            }

            if (request.Method == HttpMethod.Put && path.Contains("/users/", StringComparison.Ordinal))
            {
                var raw = await request.Content!.ReadAsStringAsync(cancellationToken);
                PutBodies.Add(JsonSerializer.Deserialize<JsonElement>(raw));

                // Gerçek Keycloak burada 204 döner — gövde eksik olsa BİLE.
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(string content) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };
    }
}
