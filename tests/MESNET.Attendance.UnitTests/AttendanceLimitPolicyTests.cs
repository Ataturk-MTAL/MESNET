using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Devamsızlık sınırı eğitim türüne göre çözülür (#183) — ve sayılar <b>mevzuattan türetilir</b>.
///
/// <para><b>Önceki hâli:</b> <c>const int limit = 20</c>; hiçbir hükümle eşleşmiyordu.
/// Bu sayı doğrudan <b>fesih tetikleyicisidir</b>: <c>AttendanceLimitExceeded</c> →
/// <c>InternshipSaga</c> → otomatik fesih.</para>
/// </summary>
public sealed class AttendanceLimitPolicyTests
{
    /// <summary>
    /// Md. 36 (5): <i>"Devamsızlık süresi <b>özürsüz 10 günü</b>… aşan öğrenciler… başarısız
    /// sayılır."</i> Sayaç yalnız mazeretsiz günleri saydığı için fıkranın özürsüz ayağı seçildi.
    /// </summary>
    [Fact]
    public void Orgun_sinir_mevzuattaki_ozursuz_gun_sayisidir()
    {
        AttendanceLimitPolicy.LimitFor("Formal").ShouldBe(10);
    }

    /// <summary>
    /// Md. 36 (5): MESEM'de işletme devamsızlığı <i>"3308'e göre kullanabileceği ücretli ve
    /// ücretsiz izin toplamından fazla olamaz"</i>. 3308 md. 26: 1 ay ücretli + 1 aya kadar
    /// ücretsiz; deponun SGK usulü 30 günlük ayıyla 30 + 30 = 60.
    /// </summary>
    [Fact]
    public void Mesem_siniri_3308_izin_toplamindan_turetilir()
    {
        AttendanceLimitPolicy.LimitFor("Mesem").ShouldBe(60);
    }

    [Fact]
    public void Buyuk_kucuk_harf_ayrimi_yapilmaz()
    {
        AttendanceLimitPolicy.LimitFor("mesem").ShouldBe(AttendanceLimitPolicy.LimitFor("Mesem"));
    }

    /// <summary>
    /// <b>Eksik veri sınırı GEVŞETMEZ.</b> Bilinmeyen ya da boş tür örgün sayılır — daha düşük
    /// eşik, yani daha erken tetikleme. Aksi hâlde eğitim türü yazılmamış bir öğrenci sessizce
    /// sınırsız devamsızlık hakkı kazanırdı.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BilinmeyenTür")]
    public void Bilinmeyen_tur_daha_DUSUK_esige_duser(string? educationType)
    {
        AttendanceLimitPolicy.LimitFor(educationType)
            .ShouldBe(AttendanceLimitPolicy.FormalUnexcusedDayLimit);
    }

    /// <summary>
    /// MESEM sınırı örgünden <b>yüksek</b> olmalı: MESEM öğrencisi 3308 izin hakkına sahiptir,
    /// örgün öğrencisinin böyle bir hakkı yoktur (#175).
    /// </summary>
    [Fact]
    public void Mesem_siniri_orgunden_yuksektir()
    {
        AttendanceLimitPolicy.MesemUnexcusedDayLimit
            .ShouldBeGreaterThan(AttendanceLimitPolicy.FormalUnexcusedDayLimit);
    }

    // ─── Parametrik: mevzuat değişirse kod değişmez ──────────────────────────────────

    /// <summary>
    /// <b>Asıl kazanım.</b> Yürürlükteki değer kayıttan gelir; mevzuat değişince kod değil
    /// <b>kayıt</b> değişir (#183, ulusal parametre — #147 deseni).
    /// </summary>
    [Fact]
    public void Yapilandirilmis_deger_sabiti_ezer()
    {
        var config = new AttendanceLimitConfig { FormalUnexcusedDayLimit = 12, MesemUnexcusedDayLimit = 45 };

        AttendanceLimitPolicy.LimitFor("Formal", config).ShouldBe(12);
        AttendanceLimitPolicy.LimitFor("Mesem", config).ShouldBe(45);
    }

    /// <summary>
    /// Kayıt yoksa mevzuattan türetilmiş başlangıç değeri kullanılır — "yapılandırma yok"
    /// sınırın kalkması anlamına gelemez.
    /// </summary>
    [Fact]
    public void Kayit_yoksa_baslangic_degerine_duser()
    {
        AttendanceLimitPolicy.LimitFor("Formal", null).ShouldBe(AttendanceLimitPolicy.FormalUnexcusedDayLimit);
        AttendanceLimitPolicy.LimitFor("Mesem", null).ShouldBe(AttendanceLimitPolicy.MesemUnexcusedDayLimit);
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
        var config = new AttendanceLimitConfig { FormalUnexcusedDayLimit = bozuk, MesemUnexcusedDayLimit = bozuk };

        AttendanceLimitPolicy.LimitFor("Formal", config).ShouldBe(AttendanceLimitPolicy.FormalUnexcusedDayLimit);
        AttendanceLimitPolicy.LimitFor("Mesem", config).ShouldBe(AttendanceLimitPolicy.MesemUnexcusedDayLimit);
    }

    /// <summary>
    /// <b>Uydurulmuş 20 geri gelmemeli.</b> Hiçbir hükümle eşleşmiyordu; yeniden konması
    /// mevzuat dayanağının kaybolduğu anlamına gelir.
    /// </summary>
    [Fact]
    public void Mevzuata_dayanmayan_eski_sabit_geri_gelmez()
    {
        AttendanceLimitPolicy.FormalUnexcusedDayLimit.ShouldNotBe(20);
        AttendanceLimitPolicy.MesemUnexcusedDayLimit.ShouldNotBe(20);
    }
}
