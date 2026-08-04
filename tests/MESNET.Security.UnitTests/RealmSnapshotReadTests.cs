using System.Net;
using System.Text;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Realm ayarlarının okunması (#195) — özellikle <b>kısmi başarısızlık</b> davranışı.
///
/// <para>Bu okumanın en büyük riski yanlış alarmdır: servis hesabının yetkisi eksikse ya da
/// Keycloak sürümü bir alanı vermiyorsa, dikkatsiz bir uygulama bunu "sapma" sanar ve her
/// açılışta gürültü çıkarır. Gürültü çıkaran kontrol kısa sürede görmezden gelinir — o zaman
/// gerçek sapma da kaçar.</para>
///
/// <para>Kilitlenen kural: okunamayan alan <c>null</c> kalır ve <c>UnreadableFields</c>'a yazılır;
/// yalnız Keycloak'a hiç ulaşılamadığında <c>Failure</c> döner. "Bilmiyorum" ile "bozuk" ayrı
/// şeylerdir.</para>
/// </summary>
public sealed class RealmSnapshotReadTests
{
    [Fact]
    public async Task Saglikli_realm_tam_okunur()
    {
        var service = CreateService(new StubHandler());

        var result = await service.GetRealmSnapshotAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.UnmanagedAttributePolicy.ShouldBe("ADMIN_EDIT");
        result.Value.WebClientIsPublic.ShouldBe(true);
        result.Value.RealmRoles.ShouldNotBeNull().ShouldContain(MesnetRoles.Parent);
        result.Value.UnreadableFields.ShouldBeEmpty();

        RealmInvariants.Verify(result.Value).ShouldBeEmpty();
    }

    [Fact]
    public async Task Sapmis_realm_okunur_ve_sapma_bulunur()
    {
        var service = CreateService(new StubHandler { Policy = "ENABLED" });

        var result = await service.GetRealmSnapshotAsync();

        RealmInvariants.Verify(result.Value)
            .ShouldHaveSingleItem().Actual.ShouldBe("ENABLED");
    }

    /// <summary>
    /// Servis hesabının <c>realm-management</c> yetkisi eksikse profil okuması 403 döner.
    /// Bu <b>sapma değildir</b> — alan okunamamıştır.
    /// </summary>
    [Fact]
    public async Task Yetkisiz_okuma_sapma_uretmez_eksik_bilgi_olarak_isaretlenir()
    {
        var service = CreateService(new StubHandler { ProfileStatus = HttpStatusCode.Forbidden });

        var result = await service.GetRealmSnapshotAsync();

        result.IsSuccess.ShouldBeTrue("Tek alanın okunamaması bütün doğrulamayı düşürmemeli.");
        result.Value.UnmanagedAttributePolicy.ShouldBeNull();
        result.Value.UnreadableFields.ShouldNotBeNull()
            .ShouldContain(f => f.Contains("403", StringComparison.Ordinal));

        // Diğer alanlar okunmaya devam etmeli.
        result.Value.WebClientIsPublic.ShouldBe(true);

        RealmInvariants.Verify(result.Value)
            .ShouldBeEmpty("Okunamayan alan sapma sayılmamalı — yanlış alarm kontrolü öldürür.");
    }

    /// <summary>
    /// Keycloak hiç ayağa kalkmadıysa token alınamaz. Burada "sapma yok" demek yanlıştır:
    /// realm doğrulanmamıştır, doğrulanıp temiz çıkmamıştır.
    /// </summary>
    [Fact]
    public async Task Keycloak_ulasilamiyorsa_basarisizlik_doner()
    {
        var service = CreateService(new StubHandler { TokenStatus = HttpStatusCode.ServiceUnavailable });

        var result = await service.GetRealmSnapshotAsync();

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Client_bulunamazsa_sapma_uretmez()
    {
        var service = CreateService(new StubHandler { WebClientFound = false });

        var result = await service.GetRealmSnapshotAsync();

        result.Value.WebClientIsPublic.ShouldBeNull();
        RealmInvariants.Verify(result.Value).ShouldBeEmpty();
    }

    // ── Test çiftleri ────────────────────────────────────────────────────────────────────

    private static KeycloakAdminService CreateService(StubHandler handler) =>
        new(new HttpClient(handler),
            new StubConfiguration(new Dictionary<string, string?>
            {
                ["Keycloak:realm"] = "mesnet",
                ["Keycloak:auth-server-url"] = "http://localhost:8080/",
                ["Keycloak:resource"] = "mesnet-api",
                ["Keycloak:credentials:secret"] = "dev-secret",
            }),
            NullLogger<KeycloakAdminService>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        public string Policy { get; init; } = "ADMIN_EDIT";
        public HttpStatusCode TokenStatus { get; init; } = HttpStatusCode.OK;
        public HttpStatusCode ProfileStatus { get; init; } = HttpStatusCode.OK;
        public bool WebClientFound { get; init; } = true;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal))
            {
                return Task.FromResult(TokenStatus == HttpStatusCode.OK
                    ? Json("""{"access_token":"test-token","expires_in":300}""")
                    : new HttpResponseMessage(TokenStatus));
            }

            if (path.EndsWith("/users/profile", StringComparison.Ordinal))
            {
                return Task.FromResult(ProfileStatus == HttpStatusCode.OK
                    ? Json($$"""{"attributes":[],"unmanagedAttributePolicy":"{{Policy}}"}""")
                    : new HttpResponseMessage(ProfileStatus));
            }

            if (path.EndsWith("/roles", StringComparison.Ordinal))
            {
                var body = string.Join(",", MesnetRoles.All.Select((r, i) =>
                    $$"""{"id":"{{i}}","name":"{{r}}"}"""));
                return Task.FromResult(Json($"[{body}]"));
            }

            if (path.EndsWith("/clients", StringComparison.Ordinal))
            {
                return Task.FromResult(Json(WebClientFound
                    ? $$"""[{"clientId":"{{RealmInvariants.WebClientId}}","publicClient":true}]"""
                    : "[]"));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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
