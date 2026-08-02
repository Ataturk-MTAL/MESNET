using MESNET.Attendance.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Ücretli izin onay zincirinin saf kuralları (#177).
///
/// <para>Zinciri ayakta tutan iki kural burada test edilir, çünkü ikisi de <b>permission ile
/// ifade edilemez</b>: <c>InstitutionManager</c> her domain wildcard'ını taşıdığı için işletme
/// adımının izni ona da gider. Adımı işletmeye bağlayan <c>business_id</c> kapsamı ve "iki adımı
/// aynı kullanıcı yapamaz" kuralı bu sınıftadır.</para>
/// </summary>
public sealed class PaidLeaveApprovalPolicyTests
{
    private static readonly Guid BusinessId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Isletme_kendi_basvurusunu_onaylayabilir()
    {
        PaidLeaveApprovalPolicy.CanBusinessApprove(BusinessId, BusinessId).ShouldBeTrue();
    }

    /// <summary>
    /// Okul müdüründe <c>business_id</c> claim'i YOKTUR. <c>attendance:*</c> wildcard'ı ona
    /// işletme adımının iznini verir; adımı yapmasını engelleyen tek şey budur.
    /// </summary>
    [Fact]
    public void Claimi_olmayan_kullanici_isletme_adimini_yapamaz()
    {
        PaidLeaveApprovalPolicy.CanBusinessApprove(null, BusinessId).ShouldBeFalse();
        PaidLeaveApprovalPolicy.CanBusinessApprove(Guid.Empty, BusinessId).ShouldBeFalse();
    }

    [Fact]
    public void Baska_isletme_basvuruyu_onaylayamaz()
    {
        PaidLeaveApprovalPolicy.CanBusinessApprove(OtherBusinessId, BusinessId).ShouldBeFalse();
    }

    /// <summary>
    /// Başvurunun işletmesi boşsa hiçbir claim eşleşmemelidir. Aksi hâlde <c>Guid.Empty</c>
    /// taşıyan bir kullanıcı, işletmesi çözülememiş bir başvuruyu onaylayabilirdi.
    /// </summary>
    [Fact]
    public void Bos_isletme_kimligi_eslesme_saymaz()
    {
        PaidLeaveApprovalPolicy.CanBusinessApprove(Guid.Empty, Guid.Empty).ShouldBeFalse();
    }

    /// <summary>
    /// Bir kullanıcı iki rolü birden taşıyabilir (izinler rollerin birleşimidir). O durumda tek
    /// kişi zincirin iki adımını da yürütür ve "iki taraflı onay" adı kalır, kendisi kalmaz.
    /// </summary>
    [Fact]
    public void Ayni_kullanici_iki_adimi_da_yapamaz()
    {
        var user = Guid.NewGuid();

        PaidLeaveApprovalPolicy.AreApproversDistinct(user, user).ShouldBeFalse();
    }

    [Fact]
    public void Farkli_kullanicilar_zinciri_tamamlar()
    {
        PaidLeaveApprovalPolicy.AreApproversDistinct(Guid.NewGuid(), Guid.NewGuid()).ShouldBeTrue();
    }

    /// <summary>Kimliksiz onay reddedilir — iki tarafı da boş bir onay eşitlik kontrolünü geçerdi.</summary>
    [Fact]
    public void Kimliksiz_onay_reddedilir()
    {
        PaidLeaveApprovalPolicy.AreApproversDistinct(Guid.Empty, Guid.NewGuid()).ShouldBeFalse();
        PaidLeaveApprovalPolicy.AreApproversDistinct(Guid.NewGuid(), Guid.Empty).ShouldBeFalse();
    }

    [Fact]
    public void Baslangic_bitisten_sonra_olamaz()
    {
        var start = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        PaidLeaveApprovalPolicy.IsRangeValid(start, start.AddDays(-1)).ShouldBeFalse();
        PaidLeaveApprovalPolicy.IsRangeValid(start, start).ShouldBeTrue();
    }

    /// <summary>
    /// Sınır, yanlış girilen bitiş tarihinin (ör. yıl hatası) binlerce devamsızlık kaydı
    /// açmasını engeller.
    /// </summary>
    [Fact]
    public void Azami_gun_sayisi_asilamaz()
    {
        var start = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var maxEnd = start.AddDays(PaidLeaveApprovalPolicy.MaxLeaveDays - 1);

        PaidLeaveApprovalPolicy.IsRangeValid(start, maxEnd).ShouldBeTrue();
        PaidLeaveApprovalPolicy.IsRangeValid(start, maxEnd.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void Gun_sayisi_iki_ucu_da_kapsar()
    {
        var start = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        PaidLeaveApprovalPolicy.DayCount(start, start).ShouldBe(1);
        PaidLeaveApprovalPolicy.DayCount(start, start.AddDays(4)).ShouldBe(5);
    }

    /// <summary>
    /// Kurum takvimindeki kısıtlı günlerde devam beklenmez; o günler için izin kaydı da
    /// açılmaz. Saat bileşeni karşılaştırmayı bozmamalıdır.
    /// </summary>
    [Fact]
    public void Kisitli_gunler_atlanir()
    {
        var start = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var holiday = start.AddDays(1).AddHours(9); // saat bileşenli kısıtlı gün

        var days = PaidLeaveApprovalPolicy.ExpandLeaveDays(start, start.AddDays(3), [holiday]);

        days.Count.ShouldBe(3);
        days.ShouldNotContain(start.AddDays(1));
        days.ShouldContain(start);
        days.ShouldContain(start.AddDays(2));
        days.ShouldContain(start.AddDays(3));
    }

    /// <summary>
    /// Hafta sonu ayrıca elenmez: MESEM'de çalışma günleri kuruma göre değişir ve hangi günün
    /// kapalı olduğu bilgisi tek yerde, kurum takvimindedir.
    /// </summary>
    [Fact]
    public void Hafta_sonu_kendiliginden_elenmez()
    {
        var saturday = new DateTime(2026, 5, 9, 0, 0, 0, DateTimeKind.Utc);
        saturday.DayOfWeek.ShouldBe(DayOfWeek.Saturday);

        var days = PaidLeaveApprovalPolicy.ExpandLeaveDays(saturday, saturday.AddDays(1), []);

        days.Count.ShouldBe(2);
    }

    [Fact]
    public void Cakisan_aralik_tespit_edilir()
    {
        var start = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

        // Uç uca değen aralıklar da çakışır — aynı güne iki izin kaydı açılırdı.
        PaidLeaveApprovalPolicy.Overlaps(start, start.AddDays(5), start.AddDays(5), start.AddDays(9))
            .ShouldBeTrue();
        PaidLeaveApprovalPolicy.Overlaps(start, start.AddDays(5), start.AddDays(6), start.AddDays(9))
            .ShouldBeFalse();
        PaidLeaveApprovalPolicy.Overlaps(start.AddDays(6), start.AddDays(9), start, start.AddDays(5))
            .ShouldBeFalse();
    }
}
