using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Devamsızlık sınırı <b>iki boyutludur</b> (#183): eğitim türü × devamsızlık türü.
///
/// <para><b>Önceki hâli:</b> <c>const int limit = 20</c>; hiçbir hükümle eşleşmiyordu. Ardından
/// tek boyuta indirilmiş bir düzeltme geldi (türe göre 10/60) ama <b>özürlü/özürsüz ayrımını
/// atlıyordu</b> — md. 36 (5) örgün için iki eşik birden koyar.</para>
///
/// <para>Bu sayılar doğrudan <b>fesih tetikleyicisidir</b>: <c>AttendanceLimitExceeded</c> →
/// <c>InternshipSaga</c> → otomatik fesih.</para>
/// </summary>
public sealed class AttendanceLimitPolicyTests
{
    private static AttendanceLimitDecision Formal(int unexcused, int total, AttendanceLimitConfig? c = null)
        => AttendanceLimitPolicy.Evaluate("Formal", unexcused, total, c);

    private static AttendanceLimitDecision Mesem(int unexcused, int total, AttendanceLimitConfig? c = null)
        => AttendanceLimitPolicy.Evaluate("Mesem", unexcused, total, c);

    // ─── Örgün: özürsüz ayak ─────────────────────────────────────────────────────────

    /// <summary>
    /// Md. 36 (5): <i>"Devamsızlık süresi <b>özürsüz 10 günü</b>… aşan öğrenciler… başarısız
    /// sayılır."</i>
    /// </summary>
    [Fact]
    public void Orgun_ozursuz_ayagi_onuncu_gunde_dolar()
    {
        Formal(unexcused: 9, total: 9).IsExceeded.ShouldBeFalse();

        var decision = Formal(unexcused: 10, total: 10);

        decision.IsExceeded.ShouldBeTrue();
        decision.Kind.ShouldBe(AttendanceLimitPolicy.UnexcusedKind);
        decision.Limit.ShouldBe(10);
    }

    // ─── Örgün: toplam ayak ──────────────────────────────────────────────────────────

    /// <summary>
    /// Md. 36 (5) ikinci ayak: <i>"…<b>toplamda 30 günü</b> aşan…"</i>. <b>Asıl kazanım budur:</b>
    /// yalnız mazeretsizi saymak bu öğrenciyi sınırın DIŞINDA bırakıyordu — 9 gün mazeretsiz
    /// (eşiğin altında) ama 30 gün toplam (eşik dolu).
    /// </summary>
    [Fact]
    public void Orgun_toplam_ayagi_ozursuz_esik_dolmadan_da_tetikler()
    {
        var decision = Formal(unexcused: 9, total: 30);

        decision.IsExceeded.ShouldBeTrue();
        decision.Kind.ShouldBe(AttendanceLimitPolicy.TotalKind);
        decision.Limit.ShouldBe(30);
        decision.Days.ShouldBe(30);
    }

    /// <summary>
    /// Ters yön: mazeretli günler <b>tek başına</b> sınırı doldurabilir. 29 gün raporlu + 1 gün
    /// mazeretsiz olan öğrenci eski modelde hiçbir eşiğe takılmıyordu.
    /// </summary>
    [Fact]
    public void Mazeretli_gunler_tek_basina_siniri_doldurabilir()
    {
        Formal(unexcused: 1, total: 30).IsExceeded.ShouldBeTrue();
    }

    /// <summary>
    /// <b>İki ayak ayrı ayrı bağlayıcıdır.</b> Toplam ayak dolmamışken mazeretsiz ayak
    /// tetiklenmeli — aksi hâlde 10 mazeretsiz günü olan öğrenci 30'a kadar beklerdi.
    /// </summary>
    [Fact]
    public void Toplam_dolmamisken_ozursuz_ayak_tetikler()
    {
        var decision = Formal(unexcused: 10, total: 12);

        decision.IsExceeded.ShouldBeTrue();
        decision.Kind.ShouldBe(AttendanceLimitPolicy.UnexcusedKind);
    }

    /// <summary>
    /// İkisi aynı anda dolduğunda gerekçe <b>özürsüz</b> yazılır: idarenin göreceği sebep,
    /// öğrencinin sorumlu olduğu ayak olmalı.
    /// </summary>
    [Fact]
    public void Iki_ayak_birden_dolunca_gerekce_ozursuzdur()
    {
        Formal(unexcused: 30, total: 30).Kind.ShouldBe(AttendanceLimitPolicy.UnexcusedKind);
    }

    [Fact]
    public void Iki_ayak_da_bosken_tetiklenmez()
    {
        var decision = Formal(unexcused: 9, total: 29);

        decision.IsExceeded.ShouldBeFalse();
        decision.Kind.ShouldBe("None");
    }

    // ─── MESEM: yalnız toplam ayak ───────────────────────────────────────────────────

    /// <summary>
    /// Md. 36 (5): MESEM'de işletme devamsızlığı <i>"3308'e göre kullanabileceği ücretli ve
    /// ücretsiz izin <b>toplamından</b> fazla olamaz"</i>. 3308 md. 26: 1 ay ücretli + 1 aya
    /// kadar ücretsiz; deponun SGK usulü 30 günlük ayıyla 30 + 30 = 60.
    /// </summary>
    [Fact]
    public void Mesem_toplam_siniri_3308_izin_toplamindan_turetilir()
    {
        Mesem(unexcused: 0, total: 59).IsExceeded.ShouldBeFalse();

        var decision = Mesem(unexcused: 0, total: 60);

        decision.IsExceeded.ShouldBeTrue();
        decision.Kind.ShouldBe(AttendanceLimitPolicy.TotalKind);
        decision.Limit.ShouldBe(60);
    }

    /// <summary>
    /// <b>MESEM'in özürsüz ayağı YOKTUR</b> — yönetmelik işletme eğitimini yalnız izin hakkı
    /// toplamıyla karşılaştırır, devamsızlık türüne bakmaz. Örgünün 10'unu MESEM'e taşımak
    /// simetri uğruna mevzuatta karşılığı olmayan bir fesih eşiği yaratırdı.
    /// </summary>
    [Fact]
    public void Mesemde_ozursuz_ayak_yoktur()
    {
        Mesem(unexcused: 40, total: 40).IsExceeded.ShouldBeFalse();
    }

    [Fact]
    public void Buyuk_kucuk_harf_ayrimi_yapilmaz()
    {
        Mesem(unexcused: 0, total: 60).IsExceeded
            .ShouldBe(AttendanceLimitPolicy.Evaluate("mesem", 0, 60).IsExceeded);
    }

    /// <summary>
    /// <b>Eksik veri sınırı GEVŞETMEZ.</b> Bilinmeyen ya da boş tür örgün sayılır — daha dar
    /// eşikler, yani daha erken tetikleme. Aksi hâlde eğitim türü yazılmamış bir öğrenci
    /// sessizce MESEM'in geniş sınırına kavuşurdu.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BilinmeyenTür")]
    public void Bilinmeyen_tur_orgun_esiklerine_duser(string? educationType)
    {
        AttendanceLimitPolicy.Evaluate(educationType, unexcusedDays: 10, totalDays: 10)
            .IsExceeded.ShouldBeTrue();
    }

    /// <summary>
    /// MESEM toplam sınırı örgün toplamından <b>yüksek</b> olmalı: MESEM öğrencisi 3308 izin
    /// hakkına sahiptir, örgün öğrencisinin böyle bir hakkı yoktur (#175).
    /// </summary>
    [Fact]
    public void Mesem_toplam_siniri_orgunden_yuksektir()
    {
        AttendanceLimitPolicy.MesemTotalDayLimit
            .ShouldBeGreaterThan(AttendanceLimitPolicy.FormalTotalDayLimit);
    }

    /// <summary>
    /// Toplam ayak mazeretsiz ayağı <b>kapsar</b>: her mazeretsiz gün aynı zamanda toplam bir
    /// gündür. Toplam sınırı mazeretsizin altına düşerse mazeretsiz ayak erişilemez hâle gelir.
    /// </summary>
    [Fact]
    public void Orgun_toplam_siniri_ozursuz_sinirindan_kucuk_olamaz()
    {
        AttendanceLimitPolicy.FormalTotalDayLimit
            .ShouldBeGreaterThanOrEqualTo(AttendanceLimitPolicy.FormalUnexcusedDayLimit);
    }

    // ─── Parametrik: mevzuat değişirse kod değişmez ──────────────────────────────────

    /// <summary>
    /// Yürürlükteki değer kayıttan gelir; mevzuat değişince kod değil <b>kayıt</b> değişir
    /// (#183, ulusal parametre — #147 deseni). <b>Her üç ayak da</b> parametriktir.
    /// </summary>
    [Fact]
    public void Yapilandirilmis_degerler_sabitleri_ezer()
    {
        var config = new AttendanceLimitConfig
        {
            FormalUnexcusedDayLimit = 12,
            FormalTotalDayLimit = 35,
            MesemTotalDayLimit = 45,
        };

        AttendanceLimitPolicy.EffectiveLimits(config)
            .ShouldBe(new AttendanceLimits(12, 35, 45));

        Formal(unexcused: 11, total: 11, config).IsExceeded.ShouldBeFalse();
        Formal(unexcused: 12, total: 12, config).Kind.ShouldBe(AttendanceLimitPolicy.UnexcusedKind);
        Formal(unexcused: 0, total: 35, config).Kind.ShouldBe(AttendanceLimitPolicy.TotalKind);
        Mesem(unexcused: 0, total: 45, config).IsExceeded.ShouldBeTrue();
    }

    /// <summary>
    /// Kayıt yoksa mevzuattan türetilmiş başlangıç değerleri kullanılır — "yapılandırma yok"
    /// sınırın kalkması anlamına gelemez.
    /// </summary>
    [Fact]
    public void Kayit_yoksa_baslangic_degerlerine_duser()
    {
        AttendanceLimitPolicy.EffectiveLimits(null).ShouldBe(new AttendanceLimits(
            AttendanceLimitPolicy.FormalUnexcusedDayLimit,
            AttendanceLimitPolicy.FormalTotalDayLimit,
            AttendanceLimitPolicy.MesemTotalDayLimit));
    }

    /// <summary>
    /// <b>Bozuk yapılandırma sınırı KALDIRMAZ.</b> 0 ya da negatif değer "her öğrenci ilk
    /// devamsızlıkta feshedilir" demek olurdu; politika başlangıç değerine düşer.
    /// (#151'in yeter sayı eşiğinde aynı tuzak aynı gerekçeyle kapatılmıştı.)
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Bozuk_deger_baslangic_degerine_duser(int bozuk)
    {
        var config = new AttendanceLimitConfig
        {
            FormalUnexcusedDayLimit = bozuk,
            FormalTotalDayLimit = bozuk,
            MesemTotalDayLimit = bozuk,
        };

        AttendanceLimitPolicy.EffectiveLimits(config).ShouldBe(new AttendanceLimits(
            AttendanceLimitPolicy.FormalUnexcusedDayLimit,
            AttendanceLimitPolicy.FormalTotalDayLimit,
            AttendanceLimitPolicy.MesemTotalDayLimit));
    }

    /// <summary>
    /// <b>Uydurulmuş 20 geri gelmemeli.</b> Hiçbir hükümle eşleşmiyordu; yeniden konması
    /// mevzuat dayanağının kaybolduğu anlamına gelir.
    /// </summary>
    [Fact]
    public void Mevzuata_dayanmayan_eski_sabit_geri_gelmez()
    {
        AttendanceLimitPolicy.FormalUnexcusedDayLimit.ShouldNotBe(20);
        AttendanceLimitPolicy.FormalTotalDayLimit.ShouldNotBe(20);
        AttendanceLimitPolicy.MesemTotalDayLimit.ShouldNotBe(20);
    }

    // ─── Sayaç ayrımı: hangi tür hangi ayağa yazılır ─────────────────────────────────

    /// <summary>
    /// Yalnız <c>Unexcused</c> mazeretsiz sayaca yazılır. <b>Ücret kesintisi ayrımıyla
    /// karıştırılmamalı:</b> ücretsiz izinde ücret kesilir (<c>AffectsSalary</c>) ama devamsızlık
    /// mazeretsiz değildir — bu iki karar farklı hükümlerden gelir.
    /// </summary>
    [Theory]
    [InlineData("Unexcused", true)]
    [InlineData("unexcused", true)]
    [InlineData("Excused", false)]
    [InlineData("HealthReport", false)]
    [InlineData("PaidLeave", false)]
    [InlineData("UnpaidLeave", false)]
    [InlineData(null, false)]
    public void Mazeretsiz_sayaci_yalniz_Unexcused_turunu_alir(string? absenceType, bool expected)
    {
        AttendanceCounterScope.CountsAsUnexcused(absenceType).ShouldBe(expected);
    }
}
