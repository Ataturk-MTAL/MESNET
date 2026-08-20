using MESNET.Payment.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Ücret kesintisine hangi devamsızlık günü sayılır (#255) — <b>tür VE durum</b> ekseni.
///
/// <para><b>Neden tek karar noktası:</b> aynı yüklem iki yerde gerekiyor. <c>PaymentSaga</c>
/// kesintiyi <i>hesaplarken</i>, <c>AbsenceTallyConsumer</c> ise <i>yeniden hesap gerekiyor mu</i>
/// diye sorarken. İkisi ayrışırsa sonuç sessizdir: tetikleyici "değişmedi" der, hesap hiç koşmaz,
/// tutar eski kalır — ne exception, ne dead letter, ne log.</para>
/// </summary>
public sealed class AbsenceDeductionPolicyTests
{
    // ─── Tür ekseni ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Unexcused")]
    [InlineData("UnpaidLeave")]
    public void Kesintiye_tabi_turler_sayilir(string tur)
    {
        AbsenceDeductionPolicy.CountsTowardDeduction(tur, "Recorded").ShouldBeTrue();
    }

    /// <summary>
    /// Sağlık raporu kesintiye tabi DEĞİLDİR (business-rules.md §6.2) — onay zincirinin sonunda
    /// kesintinin kalkmasının nedeni budur (#172).
    /// </summary>
    [Theory]
    [InlineData("HealthReport")]
    [InlineData("Excused")]
    [InlineData("PaidLeave")]
    public void Kesintiye_tabi_olmayan_turler_sayilmaz(string tur)
    {
        AbsenceDeductionPolicy.CountsTowardDeduction(tur, "Recorded").ShouldBeFalse();
    }

    /// <summary>
    /// <b>Bilinmeyen tür SAYILMAZ.</b> Kesinti öğrenci aleyhine bir hükümdür ve tanınmayan
    /// veriden doğamaz — devamsızlık <i>sınırının</i> yönünün bilerek tersi.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bilinmeyen")]
    public void Bilinmeyen_tur_sayilmaz(string? tur)
    {
        AbsenceDeductionPolicy.CountsTowardDeduction(tur, "Recorded").ShouldBeFalse();
    }

    // ─── Durum ekseni (#172, #252) ───────────────────────────────────────────────────

    /// <summary>
    /// <b>Asıl kural.</b> İşletmenin tek taraflı girdiği kayıt öğretmen onaylamadan ücreti
    /// kesemez — ödemeyi yapan taraf kendi kesintisini koyamaz.
    /// </summary>
    [Fact]
    public void Onay_bekleyen_kayit_kesintiye_sayilmaz()
    {
        AbsenceDeductionPolicy.CountsTowardDeduction("Unexcused", "Pending")
            .ShouldBeFalse("İşletme, öğretmen onayı olmadan ücret kesintisi doğuramaz.");
    }

    [Theory]
    [InlineData("Recorded")]
    [InlineData("Verified")]
    [InlineData("Corrected")]
    public void Onaylanmis_durumlar_sayilir(string durum)
    {
        AbsenceDeductionPolicy.CountsTowardDeduction("Unexcused", durum).ShouldBeTrue();
    }

    /// <summary>
    /// İki eksen birbirinden bağımsızdır: onaylanmış bir sağlık raporu yine kesmez, onay
    /// bekleyen bir mazeretsiz gün yine kesmez.
    /// </summary>
    [Fact]
    public void Iki_eksen_bagimsizdir()
    {
        AbsenceDeductionPolicy.CountsTowardDeduction("HealthReport", "Verified").ShouldBeFalse();
        AbsenceDeductionPolicy.CountsTowardDeduction("Unexcused", "Pending").ShouldBeFalse();
        AbsenceDeductionPolicy.CountsTowardDeduction("Unexcused", "Verified").ShouldBeTrue();
    }
}
