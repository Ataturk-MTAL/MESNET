using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine;

namespace MESNET.Coordination.UnitTests.Infrastructure;

/// <summary>
/// Uçların <b>hangi izinle</b> korunduğunu gerçek endpoint kaydından okur (#130, #171).
///
/// <para>Kaynak metni değil kaydın kendisi incelenir: uçlar bir test route builder'a
/// kaydedilir ve <see cref="IAuthorizeData.Policy"/> metadata'sı toplanır. Politika adı
/// doğrudan izin sabitidir (<c>SecurityServiceExtensions</c> her izin için aynı adla policy
/// üretir).</para>
/// </summary>
public static class EndpointPolicyProbe
{
    public sealed record EndpointInfo(string Method, string Route, IReadOnlyList<string> Policies);

    /// <summary>Verilen uç kaydediciden çıkan, politika taşıyan tüm uçlar.</summary>
    public static IReadOnlyList<EndpointInfo> Collect(Action<IEndpointRouteBuilder> map)
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
        map(builder);

        return
        [
            .. builder.DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .SelectMany(endpoint =>
                {
                    // Grup seviyesindeki RequireAuthorization() politikasızdır (yalnız kimlik
                    // doğrulama ister) — elenir.
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

    /// <summary>Tek bir ucun politikaları; uç kayıtlı değilse test kırmızı olur.</summary>
    public static IReadOnlyList<string> PoliciesOf(
        IReadOnlyList<EndpointInfo> endpoints, string method, string route)
    {
        var match = endpoints.SingleOrDefault(e => e.Method == method && e.Route == route);

        match.ShouldNotBeNull($"{method} {route} ucu kayıtlı değil.");
        return match.Policies;
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
