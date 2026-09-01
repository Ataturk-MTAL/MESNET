using MESNET.Common.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        // Logging: Serilog OpenTelemetry sink üzerinden gönderilir (Program.cs).
        // Built-in OTel logging devre dışı — çift log önlenir.

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Wolverine");
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Wolverine");
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Sağlık uçlarını map eder. İki uç, iki ayrı amaç — ve <b>ikisi aynı ortam kuralına tabi
    /// değildir</b>.
    ///
    /// <para><c>/alive</c> <b>her ortamda</b> açıktır. Konteynerin <c>HEALTHCHECK</c>'i buraya
    /// gelir ve yalnız "süreç ayakta mı" sorusuna cevap verir: <c>live</c> etiketli tek kontrol
    /// (<c>self</c>) koşar, gövde düz <c>Healthy</c>/<c>Unhealthy</c>'dir, hiçbir bağımlılık adı
    /// ya da hata metni sızmaz.</para>
    ///
    /// <para><c>/health</c> yalnız Development'ta açıktır. O uç <b>tüm</b> kontrolleri koşar ve
    /// yanıtında bağımlılık adları görünebilir; üretimde dışarı açmak topolojiyi bildirmek olurdu.</para>
    ///
    /// <para><b>Neden ayrıldı:</b> ikisi de Development'a kapalıyken konteyner sağlık kontrolü
    /// üretimde <b>her zaman</b> başarısızdı — imaj <c>ASPNETCORE_ENVIRONMENT=Production</c> ile
    /// koşuyor, <c>HEALTHCHECK</c> ise <c>/health</c>'e gidiyordu ve o uç map edilmemiş olduğu için
    /// 404 dönüyordu. Konteyner kalıcı olarak <c>unhealthy</c> görünür, <c>depends_on:
    /// service_healthy</c> ile bekleyen her servis süresiz bekler ve yeniden başlatma politikası
    /// olan bir orkestratör konteyneri döngüye sokar. Belirti yanıltıcıdır: API çalışır, istekleri
    /// yanıtlar, ama sağlık kontrolü kırmızıdır.</para>
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Canlılık: HER ortamda. Konteyner HEALTHCHECK'inin hedefi budur.
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        // Ayrıntılı sağlık: yalnız Development — yanıtı bağımlılık adı taşıyabilir.
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health");
        }

        return app;
    }

    /// <summary>
    /// MinIO file storage service'ini DI container'a ekler.
    /// </summary>
    public static IHostApplicationBuilder AddMinioFileStorage(this IHostApplicationBuilder builder)
    {
        // Configuration binding
        builder.Services.Configure<MinioStorageOptions>(
            builder.Configuration.GetSection(MinioStorageOptions.SectionName));

        // MinIO client registration (Singleton — internal connection pool)
        builder.Services.AddSingleton<IMinioClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioStorageOptions>>().Value;

            // Aspire endpoint "http://host:port" formatında geliyor,
            // MinIO client sadece "host:port" bekliyor — scheme'i strip et.
            //
            // Yalnız GERÇEK http/https şeması varsa strip et (#79): Uri.TryCreate("localhost:9000",
            // Absolute) true döner — "localhost" şema, "9000" path olarak parse edilir — ama "//"
            // olmadığı için Authority BOŞ string'tir. Şema kontrolü olmadan şemasız host:port
            // değeri (appsettings.Development.json'daki gibi) endpoint'i boşaltıp uygulamayı
            // açılışta düşürüyordu: "is the value of the endpoint. It can't be null or empty."
            var endpoint = options.Endpoint;
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                endpoint = uri.Authority;
                if (uri.Scheme == Uri.UriSchemeHttps)
                    options.UseSSL = true;
            }

            var minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey);

            if (options.UseSSL)
            {
                minioClient.WithSSL();
            }

            return minioClient.Build();
        });

        // FileStorageService registration
        builder.Services.AddSingleton<IFileStorageService, MinioFileStorageService>();

        // Health check registration (Aspire Dashboard'da görünür)
        builder.Services.AddHealthChecks()
            .AddCheck<MinioHealthCheck>("minio", tags: new[] { "storage", "minio" });

        return builder;
    }
}
