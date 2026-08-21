using MESNET.Common.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// E-posta gövdesine <b>işaretleme enjeksiyonu</b> kapalı (#247).
///
/// <para><b>Bulunan açık:</b> şablon MJML'den HTML'e derlendikten <b>sonra</b> yer tutucular düz
/// metin değiştirmeyle doluyor ve değerlerin bir kısmı <b>kullanıcı kontrollü</b>:
/// <c>RecipientName</c> ve <c>StudentName</c> <c>UserAccount.FullName</c>'den, yani kayıtta
/// girilen ad-soyaddan geliyor.</para>
///
/// <para><b>Neden ciddi:</b> tebligat e-postası veliye ve işletmeye gidiyor ve alıcı onu okuldan
/// gelmiş sayıyor. Adına işaretleme yazan bir kullanıcı iletiye sahte bağlantı ya da izleme
/// pikseli gömebilir — oltalama için elverişli bir yüzey. Aynı desen davet e-postasında da
/// vardı.</para>
/// </summary>
public sealed class EmailTemplateEscapingTests
{
    private static readonly IEmailTemplateService Sablon =
        new MjmlEmailTemplateService(NullLogger<MjmlEmailTemplateService>.Instance);

    private const string Saldiri = "<img src=x onerror=\"alert(1)\">";

    [Fact]
    public void Bildirimde_alici_adi_kacislanir()
    {
        var html = Sablon.RenderAbsenceNotification(
            recipientName: Saldiri, studentName: "Ayşe Yılmaz",
            stepLabel: "25. gün", legLabel: "özürsüz devamsızlık", days: 25, skippedSteps: []);

        html.ShouldNotContain("<img src=x", Case.Insensitive,
            "Kullanıcı adı e-posta gövdesine ham işaretleme sokamamalı.");
        html.ShouldContain("&lt;img");
    }

    [Fact]
    public void Bildirimde_ogrenci_adi_kacislanir()
    {
        var html = Sablon.RenderAbsenceNotification(
            recipientName: "Veli", studentName: Saldiri,
            stepLabel: "5. gün", legLabel: "toplam devamsızlık", days: 5, skippedSteps: []);

        html.ShouldNotContain("<img src=x", Case.Insensitive);
    }

    [Fact]
    public void Davet_epostasinda_ad_kacislanir()
    {
        var html = Sablon.RenderInvitation(Saldiri, "Koordinatör Öğretmen", "https://ornek/kayit");

        html.ShouldNotContain("<img src=x", Case.Insensitive,
            "Aynı desen davet e-postasında da vardı.");
    }

    /// <summary>
    /// Türkçe karakterler <b>olduğu gibi</b> kalmalı. <c>WebUtility.HtmlEncode</c> onları sayısal
    /// varlığa çevirirdi (<c>ç</c> → <c>&amp;#231;</c>); doğru render eder ama ham gövdeyi
    /// okunmaz kılar ve bu depoda Türkçe karakterler bilinçli olarak korunuyor.
    /// </summary>
    [Fact]
    public void Turkce_karakterler_bozulmaz()
    {
        var html = Sablon.RenderAbsenceNotification(
            recipientName: "Çağrı Şahin", studentName: "İbrahim Öztürk",
            stepLabel: "15. gün", legLabel: "özürsüz devamsızlık", days: 15, skippedSteps: []);

        html.ShouldContain("Çağrı Şahin");
        html.ShouldContain("İbrahim Öztürk");
    }

    /// <summary>
    /// <c>&amp;</c> ilk sırada kaçışlanmalı — sonra gelseydi kendi ürettiği varlıkları yeniden
    /// kaçışlar ve ad <c>&amp;amp;lt;</c> gibi görünürdü.
    /// </summary>
    [Fact]
    public void Ampersand_cift_kacislanmaz()
    {
        var html = Sablon.RenderAbsenceNotification(
            recipientName: "Ali & Veli", studentName: "Ogrenci",
            stepLabel: "5. gün", legLabel: "toplam devamsızlık", days: 5, skippedSteps: []);

        html.ShouldContain("Ali &amp; Veli");
        html.ShouldNotContain("&amp;amp;");
    }

    /// <summary>Atlanan kademe notu yine görünmeli — kaçışlama içeriği yutmamalı.</summary>
    [Fact]
    public void Atlanan_kademe_notu_gorunur_kalir()
    {
        var html = Sablon.RenderAbsenceNotification(
            recipientName: "Veli", studentName: "Öğrenci",
            stepLabel: "25. gün", legLabel: "özürsüz devamsızlık", days: 27,
            skippedSteps: [5, 15]);

        html.ShouldContain("zamanında yapılamamıştır");
        html.ShouldContain("5.");
        html.ShouldContain("15.");
    }
}
