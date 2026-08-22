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
/// Realm rol çözümlemesinin <b>sessiz başarı</b> regresyonu (#129).
///
/// <para>Eski davranış: istenen rol adı realm'de bulunamayınca listeden düşürülür, liste boşalırsa
/// <c>AssignRealmRolesAsync</c> <c>Result.Success()</c> dönerdi. Sonuç: Keycloak kullanıcısı sıfır
/// realm rolüyle açılır, <c>UserAccount.Roles</c> uydurma adı saklar, kullanıcı giriş yapınca
/// hiçbir şey göremez ve <b>hata mesajı da almaz</b>. #129'daki veri bozulmasının kaynağı buydu.</para>
///
/// <para>Yeni davranış: kısmi çözüm bile hatadır ve <b>hiçbir şey uygulanmaz</b> — yarım uygulanan
/// rol seti, hiç uygulanmayandan daha zor teşhis edilir.</para>
/// </summary>
public sealed class KeycloakRealmRoleResolutionTests
{
    private const string RealmName = "mesnet";

    /// <summary>Realm'de tanımlı roller — gerçek realm ile aynı olsun diye koddan beslenir.</summary>
    private static readonly string[] RealmRoles = [.. MesnetRoles.All];

    private static (KeycloakAdminService Service, StubHandler Handler) CreateService()
    {
        var handler = new StubHandler();
        var service = new KeycloakAdminService(
            new HttpClient(handler),
            new StubConfiguration(new Dictionary<string, string?>
            {
                ["Keycloak:realm"] = RealmName,
                ["Keycloak:auth-server-url"] = "http://localhost:8080/",
                ["Keycloak:resource"] = "mesnet-api",
                ["Keycloak:credentials:secret"] = "dev-secret",
            }),
            NullLogger<KeycloakAdminService>.Instance);

        return (service, handler);
    }

    [Fact]
    public async Task Cozulemeyen_rol_adi_sessizce_basari_donmez()
    {
        var (service, handler) = CreateService();

        var result = await service.AssignRealmRolesAsync("kc-user-1", ["deputy_director"]);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Security.RealmRolesUnresolved");
        result.Error.Description.ShouldContain("deputy_director");
    }

    [Fact]
    public async Task Cozulemeyen_rol_varsa_hicbir_rol_atanmaz()
    {
        var (service, handler) = CreateService();

        // Karışık liste: biri geçerli, biri değil. "Kısmi uygula" yapılmaz.
        var result = await service.AssignRealmRolesAsync(
            "kc-user-1", [MesnetRoles.Teacher, "master_trainer_yanlis"]);

        result.IsFailure.ShouldBeTrue();
        handler.RoleMappingRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Gecerli_rollerle_atama_yapilir()
    {
        var (service, handler) = CreateService();

        var result = await service.AssignRealmRolesAsync(
            "kc-user-1", [MesnetRoles.DeputyDirector, MesnetRoles.Teacher]);

        result.IsSuccess.ShouldBeTrue();
        handler.RoleMappingRequests.Count.ShouldBe(1);
        handler.RoleMappingRequests[0].Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task Rol_silmede_de_cozulemeyen_ad_sessizce_yutulmaz()
    {
        var (service, handler) = CreateService();

        var result = await service.RemoveRealmRolesAsync("kc-user-1", ["coordinator_teacher"]);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Security.RealmRolesUnresolved");
        handler.RoleMappingRequests.ShouldBeEmpty();
    }

    /// <summary>Boş liste hata değildir — çağıran zaten "değişiklik yok" demektedir.</summary>
    [Fact]
    public async Task Bos_rol_listesi_hata_uretmez_ve_istek_atmaz()
    {
        var (service, handler) = CreateService();

        var result = await service.AssignRealmRolesAsync("kc-user-1", []);

        result.IsSuccess.ShouldBeTrue();
        handler.RoleMappingRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rol_adi_buyuk_kucuk_harfe_duyarsiz_cozulur()
    {
        var (service, _) = CreateService();

        var result = await service.AssignRealmRolesAsync("kc-user-1", ["deputydirector"]);

        result.IsSuccess.ShouldBeTrue();
    }

    // ── Test çiftleri ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Keycloak Admin API'sini taklit eder: token verir, realm rollerini listeler,
    /// rol eşleme isteklerini kaydeder (gerçekten uygulanıp uygulanmadığını görmek için).
    /// </summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> RoleMappingRequests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal))
                return Task.FromResult(Json("""{"access_token":"test-token","expires_in":300}"""));

            if (path.EndsWith($"/admin/realms/{RealmName}/roles", StringComparison.Ordinal))
            {
                var body = string.Join(",", RealmRoles.Select((r, i) =>
                    $$"""{"id":"{{i}}","name":"{{r}}"}"""));
                return Task.FromResult(Json($"[{body}]"));
            }

            if (path.Contains("/role-mappings/realm", StringComparison.Ordinal))
            {
                RoleMappingRequests.Add(request);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
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
