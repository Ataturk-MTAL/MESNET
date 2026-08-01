using MESNET.Payment.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Ay içi fesihte ücret ve teşvikin gün bazlı bölüşülmesi (#154) — gün sayımı.
///
/// <para><b>Kural (sahibi tarafından karara bağlandı, 01.08.2026):</b> SGK usulü 30 günlük ay.
/// Ay tam çalışıldıysa ayın gün sayısına bakılmaksızın <b>30 gün</b>; eksik çalışıldıysa
/// <b>fiilî gün</b> sayılır. Günlük ücret <c>Taban / 30</c> olarak kalır
/// (<c>business-rules.md</c> §6.2 değişmedi).</para>
///
/// <para>Reddedilen alternatif: <c>oran = çalışılan gün / ayın gün sayısı</c>. §6.2'nin sabit
/// bölenini kısmi ayda geçersiz kılıyor ve günlük ücreti aydan aya oynatıyordu.</para>
/// </summary>
public sealed class EmploymentDaysTests
{
    private static DateTime D(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    // ── Tam ay = 30 gün ────────────────────────────────────────────────────────────

    [Fact]
    public void Ayin_tamamini_kapsayan_sozlesme_30_gun_sayilir()
    {
        // Temmuz 31 çeker; tam ay yine 30 gündür — 31. gün fazla ödeme üretmez.
        EmploymentDays.InMonth(D(2026, 1, 1), null, 2026, 7).ShouldBe(30);
    }

    [Fact]
    public void Subat_tam_calisilirsa_30_gun_sayilir()
    {
        // Şubat 28 çeker. Takvim böleni seçilseydi burada 28/30 = %93 ücret çıkardı;
        // tam ay çalışan öğrenci eksik ücret alırdı. Kararın asıl sebebi bu.
        EmploymentDays.InMonth(D(2026, 1, 1), D(2026, 12, 31), 2026, 2).ShouldBe(30);
    }

    [Fact]
    public void Ayin_ilk_ve_son_gununde_baslayip_biten_sozlesme_tam_aydir()
    {
        EmploymentDays.InMonth(D(2026, 7, 1), D(2026, 7, 31), 2026, 7).ShouldBe(30);
    }

    // ── Eksik ay = fiilî gün ───────────────────────────────────────────────────────

    [Fact]
    public void Ay_ortasinda_fesih_edilen_sozlesme_fiili_gunu_sayar()
    {
        // 1–15 Temmuz, iki uç dahil = 15 gün.
        EmploymentDays.InMonth(D(2026, 7, 1), D(2026, 7, 15), 2026, 7).ShouldBe(15);
    }

    [Fact]
    public void Ay_ortasinda_baslayan_sozlesme_fiili_gunu_sayar()
    {
        // 16–31 Temmuz, iki uç dahil = 16 gün. Fesih gününde başlayan yeni sözleşme
        // ertesi günden sayılır; aynı gün iki işletmeye birden yazılmaz.
        EmploymentDays.InMonth(D(2026, 7, 16), null, 2026, 7).ShouldBe(16);
    }

    [Fact]
    public void Ayin_son_gununde_fesih_ayin_tamami_sayilmaz_ama_tam_ucret_verir()
    {
        // 31 günlük ayda 1–30: kapsam tam değil (31'i eksik), fiilî gün 30 → tam taban.
        // Kayıt için: bu, tam ayla aynı tutarı verir ve bilinçlidir.
        EmploymentDays.InMonth(D(2026, 7, 1), D(2026, 7, 30), 2026, 7).ShouldBe(30);
    }

    [Fact]
    public void Tek_gun_calisma_bir_gun_sayilir()
    {
        EmploymentDays.InMonth(D(2026, 7, 9), D(2026, 7, 9), 2026, 7).ShouldBe(1);
    }

    // ── Kesişme yok = 0 gün ────────────────────────────────────────────────────────

    [Fact]
    public void Ay_baslamadan_kapanan_sozlesme_sifir_gun()
    {
        EmploymentDays.InMonth(D(2026, 5, 1), D(2026, 6, 30), 2026, 7).ShouldBe(0);
    }

    [Fact]
    public void Aydan_sonra_baslayan_sozlesme_sifir_gun()
    {
        EmploymentDays.InMonth(D(2026, 8, 1), null, 2026, 7).ShouldBe(0);
    }

    [Fact]
    public void Bitisi_baslangictan_once_olan_sozlesme_sifir_gun()
    {
        // Bozuk veri: negatif gün sayısı üretilmemeli.
        EmploymentDays.InMonth(D(2026, 7, 20), D(2026, 7, 10), 2026, 7).ShouldBe(0);
    }

    // ── Saat bileşeni sonucu değiştirmez ───────────────────────────────────────────

    [Fact]
    public void Gun_ici_saat_gun_sayisini_degistirmez()
    {
        // Fesih olayı TerminatedAt/EndDate'i gün ortasında taşıyabilir; gün bütünü sayılır.
        var start = new DateTime(2026, 7, 1, 23, 59, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 7, 15, 0, 1, 0, DateTimeKind.Utc);

        EmploymentDays.InMonth(start, end, 2026, 7).ShouldBe(15);
    }

    // ── Bölüşme toplamı ────────────────────────────────────────────────────────────

    [Fact]
    public void Ay_ici_devir_toplami_ayin_fiili_gun_sayisini_verir()
    {
        // 15'inde fesih + 16'sında yeni sözleşme: 15 + 16 = 31 gün.
        // 31 günlük ayda toplam tabanı AŞAR (31/30) — bilinerek kabul edildi (#154):
        // her işveren kendi istihdam günü için sabit günlük ücreti öder, kırpma yapılmaz.
        var ayrilan = EmploymentDays.InMonth(D(2026, 7, 1), D(2026, 7, 15), 2026, 7);
        var yeni = EmploymentDays.InMonth(D(2026, 7, 16), null, 2026, 7);

        (ayrilan + yeni).ShouldBe(31);
    }
}
