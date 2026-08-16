using MESNET.Common.Shared.Telemetry;

namespace MESNET.Presentation;

/// <summary>
/// Tarayıcıdaki hataların sunucu loguna ulaşması (#144).
///
/// <para><b>Neden gerekti:</b> WebUI'daki tanı satırlarının tamamı yalnız kullanıcının kendi
/// konsoluna yazıyor ve orada ölüyordu. #136 (süresi dolmuş token → sonsuz yeniden giriş
/// döngüsü) bunun somut maliyeti: kullanıcı <b>beyaz ekran</b> görüyor, "açılmıyor" diyebiliyor;
/// tarayıcı konsolunda teşhis zaten yazılıydı ama kimse görmedi. Hata ancak <b>sunucu</b>
/// logundaki ikinci bir izden fark edildi. Sunucuya hiç istek üretmeyen bir hata (render
/// çökmesi, boot hatası, yakalanmamış promise reddi) bugün iz bırakmadan kayboluyordu.</para>
///
/// <para><b>Yeni altyapı kurulmaz:</b> kayıt mevcut Serilog boru hattına yazılır ve oradan
/// OTLP sink'ine gider. Marten belgesi ya da modül şeması <b>oluşturulmaz</b>.</para>
/// </summary>
public static class TelemetryEndpoints
{
    // Anonim uç sınırsız metin kabul etmemeli; alan başına sınırlar.
    private const int MaxMessageLength = 1_000;
    private const int MaxStackLength = 4_000;
    private const int MaxShortFieldLength = 300;

    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/telemetry").WithTags("Telemetri");

        group.MapPost("/client-errors", PostClientError)
            // KİMLİK DOĞRULAMASI İSTEMEZ — bilinçli. #136'daki hatanın tamamı oturum
            // kurulamadığı için yaşandı ve token isteyen bir uç, en çok ihtiyaç duyulan anda
            // çalışmazdı. Bu uç HİÇBİR Marten belgesine dokunmaz (yalnız ILogger), dolayısıyla
            // AnonymousEndpointDriftTests'in aradığı kiracı riski burada yoktur.
            .AllowAnonymous()
            // Hız sınırı ZORUNLU. #136'da istisna saniyede bir tekrarlıyordu ve aynı döngü
            // istemcide olsaydı sınırsız bir uç kendi kendine DoS olurdu.
            .RequireRateLimiting("ClientTelemetry");

        return app;
    }

    private static IResult PostClientError(
        ClientErrorReport report, ILogger<ClientErrorReport> logger, HttpContext http)
    {
        // Gövdeyi tamamen istemci belirliyor (uç anonim) — her alan gizleme+kırpmadan geçer.
        // KVKK: token, e-posta, kimlik numarası loga yazılmaz.
        var message = ClientErrorSanitizer.Clean(report.Message, MaxMessageLength);
        var stack = ClientErrorSanitizer.Clean(report.Stack, MaxStackLength);
        var path = ClientErrorSanitizer.Clean(report.Path, MaxShortFieldLength);
        var appVersion = ClientErrorSanitizer.Clean(report.AppVersion, MaxShortFieldLength);
        var correlationId = ClientErrorSanitizer.Clean(report.CorrelationId, MaxShortFieldLength);

        // UserAgent gövdeden DEĞİL başlıktan okunur: istemcinin uydurmasına gerek yok ve
        // gövdedeki bir alan daha az demek, temizlenecek bir yüzey daha az demek.
        var userAgent = ClientErrorSanitizer.Truncate(
            http.Request.Headers.UserAgent.ToString(), MaxShortFieldLength);

        // Seviye istemciden gelir ama SERBEST DEĞİL — bilinmeyen değer Error'a düşer, yoksa
        // istemci kendi hatasını Trace'e yazıp görünmez kılabilirdi.
        var level = report.Level?.ToLowerInvariant() switch
        {
            "warn" or "warning" => LogLevel.Warning,
            "info" or "information" => LogLevel.Information,
            _ => LogLevel.Error,
        };

        logger.Log(level,
            "İstemci hatası: {ClientMessage} | yol: {ClientPath} | sürüm: {AppVersion} "
            + "| korelasyon: {CorrelationId} | tarayıcı: {UserAgent} | yığın: {ClientStack}",
            message, path, appVersion, correlationId, userAgent, stack);

        // 202: kayıt alındı, sonucu istemciyi ilgilendirmiyor. İstemci yanıtı beklemiyor ve
        // hata dönse bile TEKRAR DENEMEMELİ (bkz. logger.ts) — döngü riski.
        return Results.Accepted();
    }
}

/// <summary>
/// Tarayıcıdan gelen hata kaydı. Alanlar <b>opsiyoneldir</b>: boot çökmesinde istemci bunların
/// hepsini toplayamayabilir ve eksik alan yüzünden kaydın reddedilmesi, tam da en çok ihtiyaç
/// duyulan anda kaydı kaybetmek olurdu.
/// </summary>
public sealed record ClientErrorReport(
    string? Level,
    string? Message,
    string? Stack,
    string? Path,
    string? AppVersion,
    string? CorrelationId);
