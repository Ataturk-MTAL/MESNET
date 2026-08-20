using System.Reflection;
using MESNET.Attendance.Application.Handlers;
using Wolverine;
using Wolverine.Marten;
using MESNET.Attendance.Shared.Events;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Sayılabilir küme değiştiren her olay sınırı <b>yeniden ölçtürür</b> (#252).
///
/// <para><b>Neden bu test var:</b> onay bekleyen kayıt sayaçtan çıkarıldı. Yalnız
/// <c>AttendanceMarked</c> dinlenseydi açık kapanırken yerine ters yönde bir açık açılırdı:
/// işletmenin bildirdiği devamsızlık, öğretmen onayladıktan sonra da <b>hiçbir zaman</b>
/// sayılmaz ve mevzuatın emrettiği fesih sessizce hiç tetiklenmezdi. Süre dolunca kendiliğinden
/// onay <b>yoktur</b> — <c>business-rules.md</c> §5.7'deki <c>AutoApproveExpiredAttendance</c>
/// işi kodda yazılmamıştır, yani <c>Pending</c> kayıt onaylanana kadar <c>Pending</c> kalır.</para>
///
/// <para><b>Tip adına bağlanmaz:</b> tarama Application katmanının tamamında, ilk parametre
/// tipine göre yapılır. Handler bir gün bölünürse ya da adı değişirse test yine doğru soruyu
/// sorar — "bu olay limiti yeniden ölçtürüyor mu".</para>
///
/// <para><b>Handler'ın VAR OLMASI YETMEZ — olayın YAYINLANMASI da gerekir.</b> Bu testin ilk
/// hâli yalnız imza tarıyordu ve gerçek bir açığı <b>yanlış yeşille</b> geçirdi:
/// <c>[AggregateHandler]</c> dönüşü cascading mesaj değildir (Wolverine.Marten dönüş eylemini
/// <c>EventCaptureActionSource</c> ile değiştirir, nesne yalnız akışa eklenir), dolayısıyla
/// <c>AttendanceApproved</c> hiçbir handler'a ulaşmıyordu. Aşağıdaki ikinci kilit bu yüzden
/// var.</para>
/// </summary>
public sealed class AttendanceLimitRecheckCoverageTests
{
    private static readonly Assembly ApplicationKatmani = typeof(CheckAttendanceLimitHandler).Assembly;

    private static readonly string[] HandlerMetotAdlari = ["Handle", "HandleAsync", "Consume", "ConsumeAsync"];

    /// <summary>
    /// Bu olay tipini ilk parametre olarak alan ve <see cref="AttendanceLimitExceeded"/>
    /// üretebilen bir handler var mı.
    /// </summary>
    private static bool LimitiYenidenOlcer(Type olayTipi) => ApplicationKatmani
        .GetTypes()
        .Where(t => t.Name.EndsWith("Handler") || t.Name.EndsWith("Consumer"))
        .SelectMany(t => t.GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        .Where(m => HandlerMetotAdlari.Contains(m.Name))
        .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType == olayTipi)
        .Any(LimitOlayiUretir);

    private static bool LimitOlayiUretir(MethodInfo metot)
    {
        var donus = metot.ReturnType;

        if (donus.IsGenericType && donus.GetGenericTypeDefinition() == typeof(Task<>))
            donus = donus.GetGenericArguments()[0];

        // Cascading mesaj tuple ile de dönebilir — tuple'ın herhangi bir ayağı sayılır.
        var adaylar = donus.IsGenericType && donus.FullName?.StartsWith("System.ValueTuple") == true
            ? donus.GetGenericArguments()
            : [donus];

        return adaylar.Any(t => t == typeof(AttendanceLimitExceeded));
    }

    /// <summary>Giriş — bugünkü davranış; taramanın kendisinin çalıştığının kanıtı.</summary>
    [Fact]
    public void Devamsizlik_girisi_limiti_olcer()
    {
        LimitiYenidenOlcer(typeof(AttendanceMarked)).ShouldBeTrue();
    }

    /// <summary><b>Asıl regresyon.</b> Onay kaydı sayaca sokan olaydır; sınır orada yeniden ölçülmeli.</summary>
    [Fact]
    public void Onay_limiti_yeniden_olcer()
    {
        LimitiYenidenOlcer(typeof(AttendanceApproved)).ShouldBeTrue(
            "Onay kaydı sayaca sokar; ölçülmezse işletmenin bildirdiği devamsızlık hiçbir zaman "
            + "feshe yol açmaz ve mevzuatın emri sessizce uygulanmaz.");
    }

    /// <summary>
    /// Düzeltme <b>türü</b> değiştirir (mazeretli → mazeretsiz) ve mazeretsiz ayak 10 günde
    /// dolduğu için sınırı düzeltmenin kendisi doldurabilir.
    /// </summary>
    [Fact]
    public void Duzeltme_limiti_yeniden_olcer()
    {
        LimitiYenidenOlcer(typeof(AttendanceCorrected)).ShouldBeTrue(
            "Tür değiştiren düzeltme sayacı artırabilir; ölçülmezse değişim hükümsüz kalır.");
    }

    // ─── İkinci kilit: olay gerçekten mesaj olarak yayınlanıyor mu ───────────────────

    /// <summary>
    /// Sınır ölçümünü besleyen olayı üreten <c>[AggregateHandler]</c>, olayı <b>yayınlamak</b>
    /// zorundadır — yalnız akışa yazmak yetmez.
    ///
    /// <para><b>Neden bu test var:</b> <c>[AggregateHandler]</c> iş akışında dönüş değeri
    /// cascading mesaj DEĞİLDİR; Wolverine.Marten handler'ın dönüş eylemini
    /// <c>EventCaptureActionSource</c> ile değiştirir ve dönen nesneyi yalnız
    /// <c>IEventStream&lt;T&gt;.AppendOne</c> ile akışa ekler. Olay yönlendirmesi
    /// (<c>EventForwardingToWolverine</c>) da bilerek kapalıdır. Yani handler bare olay
    /// döndürürse <c>CheckAttendanceLimitHandler</c>'ın onay/düzeltme girişleri sessizce
    /// <b>ölü</b> kalır ve mevzuatın emrettiği fesih hiç tetiklenmez.</para>
    ///
    /// <para>Doğru şekil: <c>(Events, OutgoingMessages)</c> — ilki akışa yazılır, ikincisi
    /// yayınlanır.</para>
    /// </summary>
    /// <remarks>
    /// <b>Simetri şarttır.</b> Yalnız onay yayınlansaydı ücret kesintisini <i>koyan</i> yol
    /// çalışır, <i>kaldıran</i> yollar (rapor onayı, rapor girişi, silme) ölü kalırdı: geçerli
    /// raporu olan ya da kaydı silinen öğrencinin ücreti kesilir ve hiçbir arayüzden geri
    /// alınamazdı. Liste bu yüzden altı handler'ı birden kilitler.
    /// </remarks>
    [Theory]
    [InlineData(typeof(ApproveAttendanceHandler))]
    [InlineData(typeof(CorrectAttendanceHandler))]
    [InlineData(typeof(ApproveHealthReportHandler))]
    [InlineData(typeof(AttachHealthReportHandler))]
    [InlineData(typeof(DeleteAttendanceHandler))]
    [InlineData(typeof(VerifyAttendanceHandler))]
    public void Aggregate_handler_olayi_mesaj_olarak_da_yayinlar(Type handlerTipi)
    {
        var metot = handlerTipi
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Single(m => HandlerMetotAdlari.Contains(m.Name));

        metot.GetCustomAttributes(typeof(AggregateHandlerAttribute), inherit: false)
            .ShouldNotBeEmpty($"{handlerTipi.Name} aggregate iş akışında olmalı.");

        var donus = metot.ReturnType;

        // async handler'da dönüş Task<(Events, OutgoingMessages)>; tuple bir kat içeride.
        if (donus.IsGenericType && donus.GetGenericTypeDefinition() == typeof(Task<>))
            donus = donus.GetGenericArguments()[0];

        var donusAyaklari = donus.IsGenericType ? donus.GetGenericArguments() : [donus];

        donusAyaklari.ShouldContain(typeof(OutgoingMessages),
            $"{handlerTipi.Name} olayı yalnız akışa yazarsa hiçbir handler'a ulaşmaz; "
            + "sınır yeniden ölçülmez ve fesih sessizce hiç tetiklenmez.");

        donusAyaklari.ShouldContain(typeof(Events),
            $"{handlerTipi.Name} olayı akışa da yazmalı; yalnız yayınlarsa agrega güncellenmez.");
    }
}
