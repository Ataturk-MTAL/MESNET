using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Kademeli devamsızlık bildirimi — <b>5., 15., 25.</b> gün (#247, md. 36 (4)).
///
/// <para><b>Yasal dayanak:</b> işletmede mesleki eğitimde devamsızlığın 5., 15. ve 25. gününde
/// veliye ve işletmeye yazılı bildirim yapılır; 18 yaşından büyük öğrencide öğrencinin kendisine
/// de. Fıkranın amacı ailenin, md. 36 (5) feshi gelmeden önce durumu öğrenmesidir.</para>
///
/// <para><b>Neden bu testler kritik:</b> tebligat eksikliği de fazlalığı da sessizdir. Eksikse
/// aile hiç uyarılmaz ve fesih habersiz gelir; fazlaysa her yeni devamsızlık kaydında aynı
/// bildirim yeniden gider (sayaç dönem içinde sıfırlanmıyor, #242).</para>
/// </summary>
public sealed class AbsenceNotificationPolicyTests
{
    private const string Mazeretsiz = AbsenceNotificationLeg.Unexcused;
    private const string Toplam = AbsenceNotificationLeg.Total;

    // ─── Kademe tespiti ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void Esik_altinda_bildirim_yok(int gun)
    {
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, gun, 0).ShouldBeNull();
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(14, 5)]
    [InlineData(15, 15)]
    [InlineData(24, 15)]
    [InlineData(25, 25)]
    [InlineData(40, 25)]
    public void Ulasilan_en_yuksek_kademe_bildirilir(int gun, int beklenenKademe)
    {
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, gun, 0)!.Step.ShouldBe(beklenenKademe);
    }

    /// <summary>
    /// <b>Asıl idempotency kuralı.</b> Aynı kademe iki kez bildirilmez — sayaç dönem içinde
    /// sıfırlanmadığı için eşik dolduktan sonraki her kayıt/onay yeniden tetiklerdi.
    /// </summary>
    [Fact]
    public void Ayni_kademe_iki_kez_bildirilmez()
    {
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 7, lastNotifiedStep: 5).ShouldBeNull();
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 5, lastNotifiedStep: 5).ShouldBeNull();
    }

    [Fact]
    public void Sonraki_kademe_bildirilir()
    {
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 15, lastNotifiedStep: 5)!.Step.ShouldBe(15);
    }

    /// <summary>
    /// Sıçrama: haftalık toplu giriş ya da biriken kayıtların toplu onayı sayaç bir hamlede
    /// birden çok kademeyi geçirebilir. Yalnız en yüksek kademe bildirilir; atlananlar kayda
    /// geçer.
    /// </summary>
    [Fact]
    public void Sicramada_yalniz_en_yuksek_kademe_bildirilir_atlananlar_kayda_gecer()
    {
        var karar = AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 27, lastNotifiedStep: 0)!;

        karar.Step.ShouldBe(25);
        karar.Days.ShouldBe(27);
        karar.SkippedSteps.ShouldBe([5, 15],
            "Zamanında yapılamayan bildirimler tebligatta görünebilmeli.");
    }

    [Fact]
    public void Kismi_sicramada_yalniz_gecilen_kademeler_atlanmis_sayilir()
    {
        var karar = AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 27, lastNotifiedStep: 5)!;

        karar.Step.ShouldBe(25);
        karar.SkippedSteps.ShouldBe([15]);
    }

    /// <summary>
    /// Sayaç düzeltme/silme/rapor onayıyla DÜŞEBİLİR. Kademe geri alınmaz: yapılmış bir tebligat
    /// yapılmamış sayılamaz. Sayaç düşüp yeniden aynı kademeye çıkarsa ikinci bildirim gitmez.
    /// </summary>
    [Fact]
    public void Sayac_dusup_yeniden_ciksa_da_ikinci_bildirim_gitmez()
    {
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 3, lastNotifiedStep: 15).ShouldBeNull();
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 16, lastNotifiedStep: 15).ShouldBeNull();
    }

    /// <summary>İki ayak bağımsızdır ve aynı gün ikisi birden dolabilir.</summary>
    [Fact]
    public void Iki_ayak_bagimsizdir()
    {
        AbsenceNotificationPolicy.Evaluate(Mazeretsiz, 5, 0)!.Leg.ShouldBe(Mazeretsiz);
        AbsenceNotificationPolicy.Evaluate(Toplam, 5, 0)!.Leg.ShouldBe(Toplam);
    }

    // ─── Defter ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Defter_ayaklari_ayri_tutar()
    {
        var defter = new AbsenceNotificationLog();

        defter.Advance(Mazeretsiz, 15, DateTime.UtcNow);

        defter.StepFor(Mazeretsiz).ShouldBe(15);
        defter.StepFor(Toplam).ShouldBe(0, "Bir ayağın ilerlemesi diğerini etkilememeli.");
    }

    [Fact]
    public void Defter_kademeyi_geri_almaz()
    {
        var defter = new AbsenceNotificationLog();
        defter.Advance(Toplam, 25, DateTime.UtcNow);

        defter.Advance(Toplam, 5, DateTime.UtcNow);

        defter.StepFor(Toplam).ShouldBe(25, "Yapılmış tebligat yapılmamış sayılamaz.");
    }

    // ─── 18 yaş kuralı ───────────────────────────────────────────────────────────────

    private static readonly DateTime Bugun = new(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Onsekiz_yasini_dolduran_ogrenciye_bildirilir()
    {
        AbsenceNotificationPolicy.ShouldNotifyStudent(new DateTime(2008, 8, 21), Bugun)
            .ShouldBeTrue();
    }

    [Fact]
    public void Onsekiz_yasindan_kucuk_ogrenciye_ayrica_bildirilmez()
    {
        AbsenceNotificationPolicy.ShouldNotifyStudent(new DateTime(2008, 8, 22), Bugun)
            .ShouldBeFalse("Veli ve işletme ayakları zaten koşulsuz; öğrenci ayağı yalnız 18+.");
    }

    /// <summary>
    /// <b>Doğum tarihi bilinmiyorsa GÖNDERİLİR.</b> <c>StudentProfile.BirthDate</c> nullable ve
    /// kayıtta zorunlu değil — bilinmeyen doğum tarihi kenar durum değil, yaygın hâl. Gönderme
    /// yönü güvenlidir: öğrenci ayağı bir alıcı EKLER, zorunlu alıcı KALDIRMAZ.
    /// </summary>
    [Fact]
    public void Dogum_tarihi_bilinmiyorsa_gonderilir()
    {
        AbsenceNotificationPolicy.ShouldNotifyStudent(null, Bugun).ShouldBeTrue();
    }

    [Fact]
    public void Bozuk_gelecek_tarihte_de_gonderilir()
    {
        AbsenceNotificationPolicy.ShouldNotifyStudent(new DateTime(2030, 1, 1), Bugun)
            .ShouldBeTrue("Eksik/bozuk veri alıcıyı düşürmemeli.");
    }
}
