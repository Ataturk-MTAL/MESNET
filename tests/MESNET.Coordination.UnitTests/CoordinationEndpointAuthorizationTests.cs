using MESNET.Common.Shared.Security;
using MESNET.Coordination.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Koordinasyon uçlarının <b>hangi izinle</b> korunduğunun kilidi (#130).
///
/// <para>Kaynak metni değil gerçek endpoint kaydı incelenir: uçlar
/// <c>MapCoordinationEndpoints</c> ile bir test route builder'a kaydedilir ve
/// <see cref="IAuthorizeData.Policy"/> metadata'sı okunur. Politika adı doğrudan izin
/// sabitidir (<c>SecurityServiceExtensions</c> her izin için aynı adla policy üretir).</para>
///
/// <para>Kilitlenen karar: <c>POST /config</c> kurum düzeyi izin ister,
/// <c>GET /config</c> ise dağıtım izniyle <b>açık kalır</b> — alan şefi yapılandırmayı
/// görür, değiştiremez. Okumayı da kısıtlayan biri kırmızı test görür.</para>
/// </summary>
public sealed class CoordinationEndpointAuthorizationTests
{
    private const string ConfigRoute = "/api/coordination/teachers/config";

    [Fact]
    public void Yapilandirma_yazma_ucu_kurum_duzeyi_izin_ister()
    {
        PoliciesOf(HttpMethods.Post, ConfigRoute)
            .ShouldContain(Permissions.Institution.CoordinationConfigManage);
    }

    /// <summary>
    /// Dağıtım izni <b>tek başına</b> yazmaya yetmemeli. Alan şefi bu izni
    /// <c>department:*</c> ile taşır; uçta kalsaydı kısıt hiç uygulanmazdı.
    /// </summary>
    [Fact]
    public void Yapilandirma_yazma_ucu_dagitim_iznini_kabul_etmez()
    {
        PoliciesOf(HttpMethods.Post, ConfigRoute)
            .ShouldNotContain(Permissions.DepartmentHead.Distribution);
    }

    /// <summary>Regresyon: okuma kısıtlanmadı (#126'nın "okuma açık, yazma kapalı" kararı).</summary>
    [Fact]
    public void Yapilandirma_okuma_ucu_alan_sefine_acik_kalir()
    {
        var policies = PoliciesOf(HttpMethods.Get, ConfigRoute);

        policies.ShouldContain(Permissions.DepartmentHead.Distribution);
        policies.ShouldNotContain(Permissions.Institution.CoordinationConfigManage);
    }

    /// <summary>
    /// Yeni izin <b>yalnız</b> yapılandırma yazma ucuna uygulanmalı — koordinasyonun geri
    /// kalanı alan şefine açık kalır. İzni başka uçlara yayan biri, alan şefini sessizce
    /// kilitlemiş olur.
    /// </summary>
    [Fact]
    public void Yeni_izin_baska_hicbir_uca_uygulanmaz()
    {
        var carriers = AuthorizedEndpoints()
            .Where(e => e.Policies.Contains(Permissions.Institution.CoordinationConfigManage))
            .Select(e => $"{e.Method} {e.Route}")
            .ToList();

        carriers.ShouldBe([$"{HttpMethods.Post} {ConfigRoute}"]);
    }

    // ── Yardımcılar ──

    private static IReadOnlyList<string> PoliciesOf(string method, string route)
    {
        var match = AuthorizedEndpoints()
            .SingleOrDefault(e => e.Method == method && e.Route == route);

        match.ShouldNotBeNull($"{method} {route} ucu kayıtlı değil.");
        return match.Policies;
    }

    private sealed record EndpointInfo(string Method, string Route, IReadOnlyList<string> Policies);

    private static IReadOnlyList<EndpointInfo> AuthorizedEndpoints()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        // Uç imzalarındaki IMessageBus servis olarak tanınmalı; aksi hâlde parametre
        // çıkarımı onu gövde (body) sanır ve endpoint kurulumu patlar. Örnek hiç
        // çözümlenmez — metadata çıkarımı yalnız kaydın varlığına bakar.
        services.AddSingleton<IMessageBus>(
            _ => throw new NotSupportedException("Metadata testi mesaj yolu çözümlemez."));
        using var provider = services.BuildServiceProvider();

        var builder = new TestEndpointRouteBuilder(provider);
        builder.MapCoordinationEndpoints();

        return
        [
            .. builder.DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .SelectMany(endpoint =>
                {
                    // Politika adı = izin sabiti. Grup seviyesindeki RequireAuthorization()
                    // politikasızdır (yalnız kimlik doğrulama ister) — elenir.
                    var policies = endpoint.Metadata
                        .OfType<IAuthorizeData>()
                        .Select(data => data.Policy)
                        .Where(policy => !string.IsNullOrWhiteSpace(policy))
                        .Select(policy => policy!)
                        .ToList();

                    var methods = endpoint.Metadata
                        .GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];

                    return methods.Select(method => new EndpointInfo(
                        method, endpoint.RoutePattern.RawText ?? string.Empty, policies));
                })
        ];
    }

    /// <summary>
    /// <see cref="IEndpointRouteBuilder"/>'ın en küçük gerçeklemesi — tüm bir
    /// <c>WebApplication</c> ayağa kaldırmadan uç kayıtlarını toplar.
    /// </summary>
    private sealed class TestEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder
    {
        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }
}
