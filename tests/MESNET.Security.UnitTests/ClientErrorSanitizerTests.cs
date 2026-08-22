using MESNET.Common.Shared.Telemetry;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Tarayıcıdan gelen hata kaydının <b>ne taşıyabileceği</b> (#144).
///
/// <para><b>Neden Security testlerinde:</b> buradaki kural gizleme/temizlemedir — token ve
/// kişisel veri sunucu loguna sızmamalı. Çözüm <c>Common.Shared</c>'da yaşıyor ve ona ait ayrı
/// bir birim test projesi yok (<c>AcademicYearTests</c>'in Reporting'e konması ile aynı gerekçe);
/// konu güvenlik olduğu için bu proje seçildi.</para>
///
/// <para><b>Neden gerekli:</b> uç <b>anonimdir</b> — kimlik doğrulaması istese en çok ihtiyaç
/// duyulan anda (#136'daki gibi oturum hiç kurulamazken) çalışmazdı. Yani gövdeyi istemci
/// belirler ve sunucu ona güvenemez. <c>useNotify.ts</c> bugün ham API hata nesnesini konsola
/// basıyor; aynısı sunucuya gönderilirse içinde ne olduğu denetlenmelidir.</para>
/// </summary>
public sealed class ClientErrorSanitizerTests
{
    // ─── Gizleme: token ──────────────────────────────────────────────────────────────

    /// <summary>
    /// JWT en tehlikeli sızıntıdır: log'a düşen geçerli bir token, o logu okuyabilen herkese
    /// kullanıcının oturumunu verir.
    /// </summary>
    [Fact]
    public void Jwt_gizlenir()
    {
        var jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NSJ9.abcDEF123-_x";

        var result = ClientErrorSanitizer.Redact($"401 döndü, token: {jwt}");

        result.ShouldNotContain(jwt);
        result.ShouldContain(ClientErrorSanitizer.RedactionMarker);
    }

    [Fact]
    public void Bearer_basligi_gizlenir()
    {
        ClientErrorSanitizer.Redact("Authorization: Bearer abc.def.ghi")
            .ShouldNotContain("abc.def.ghi");
    }

    // ─── Gizleme: kişisel veri ───────────────────────────────────────────────────────

    [Fact]
    public void Eposta_gizlenir()
    {
        var result = ClientErrorSanitizer.Redact("kullanıcı ahmet.yilmaz@okul.meb.gov.tr bulunamadı");

        result.ShouldNotContain("ahmet.yilmaz@okul.meb.gov.tr");
        result.ShouldContain(ClientErrorSanitizer.RedactionMarker);
    }

    /// <summary>
    /// 11 haneli sayı dizisi T.C. kimlik numarası olabilir. Gerçekten kimlik mi diye bakılmaz —
    /// yanlış gizleme zararsız, yanlış sızdırma değil.
    /// </summary>
    [Fact]
    public void Onbir_haneli_sayi_gizlenir()
    {
        ClientErrorSanitizer.Redact("tcKimlikNo: 12345678901 geçersiz")
            .ShouldNotContain("12345678901");
    }

    /// <summary>
    /// Kısa sayılar gizlenmez — HTTP durum kodu, satır numarası, sayaç gibi tanı için gerekli
    /// bilgiler kaybolursa kayıt işe yaramaz hâle gelir.
    /// </summary>
    [Fact]
    public void Kisa_sayilar_korunur()
    {
        var result = ClientErrorSanitizer.Redact("HTTP 401, 3. deneme, satır 214");

        result.ShouldContain("401");
        result.ShouldContain("214");
    }

    /// <summary>Tanı bilgisi korunmalı — her şeyi gizleyen bir temizleyici kaydı çöpe çevirir.</summary>
    [Fact]
    public void Tani_metni_korunur()
    {
        const string mesaj = "[Auth] /auth/me henüz hazır değil (durum: 401), 1.5s sonra tekrar";

        ClientErrorSanitizer.Redact(mesaj).ShouldBe(mesaj);
    }

    // ─── Kırpma ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Uç anonim ve gövdeyi istemci belirliyor; sınırsız metin kabul etmek log hattını
    /// şişirmenin (ve OTLP sink'ini boğmanın) kolay yolu olurdu.
    /// </summary>
    [Fact]
    public void Uzun_mesaj_kirpilir()
    {
        var result = ClientErrorSanitizer.Truncate(new string('x', 5000), 1000);

        result.Length.ShouldBeLessThanOrEqualTo(1000 + ClientErrorSanitizer.TruncationSuffix.Length);
        result.ShouldEndWith(ClientErrorSanitizer.TruncationSuffix);
    }

    [Fact]
    public void Kisa_mesaj_kirpilmaz()
    {
        ClientErrorSanitizer.Truncate("kısa", 1000).ShouldBe("kısa");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Bos_deger_bozulmaz(string? value)
    {
        ClientErrorSanitizer.Redact(value).ShouldBe(value);
        ClientErrorSanitizer.Truncate(value, 100).ShouldBe(value);
    }

    // ─── Birleşik ────────────────────────────────────────────────────────────────────

    /// <summary>Temizleme hem gizler hem kırpar — biri atlanırsa diğeri işe yaramaz.</summary>
    [Fact]
    public void Temizleme_hem_gizler_hem_kirpar()
    {
        var uzunVeGizli = "token eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.sig " + new string('y', 5000);

        var result = ClientErrorSanitizer.Clean(uzunVeGizli, 1000);

        result!.ShouldNotContain("eyJhbGciOiJIUzI1NiJ9");
        result.Length.ShouldBeLessThanOrEqualTo(1000 + ClientErrorSanitizer.TruncationSuffix.Length);
    }
}
