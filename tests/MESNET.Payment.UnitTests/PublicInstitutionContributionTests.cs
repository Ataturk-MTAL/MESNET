using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Kamu kurumlarına devlet katkısı ödenmemesi regresyonu (#157).
///
/// <para><b>Kanun:</b> 3308 sayılı Kanun Geçici Madde 12 — <b>"Kamu kurum ve kuruluşlarına
/// Devlet katkısı ödenmez."</b> Özet ve kaynak:
/// <c>src/Docs/docs/architecture/3308-kanun-ozeti.md</c></para>
///
/// <para><b>Hata neydi:</b> <c>GovernmentContributionType.PublicInstitution</c> enum değeri
/// tanımlıydı ama depo genelinde <b>hiçbir yerde kullanılmıyordu</b> — hiçbir kod atamıyor,
/// hiçbir kod kontrol etmiyordu. Kamu kurumunda staj yapan öğrenci için devlet katkısı, kanun
/// ödenmemesini emrettiği hâlde özel işletme gibi hesaplanıyordu; yani sistem hak edilmeyen
/// bir kamu ödemesi üretiyordu.</para>
///
/// <para>Enum'un yarım bağlı durması #152'nin bedelini ödettiği sınıfın aynısıydı: tanımlı
/// ama bağlı olmayan durum, uygulanmış görünüp uygulanmıyor.</para>
/// </summary>
public sealed class PublicInstitutionContributionTests
{
    private const decimal MinimumWage = 20_000m;

    private static SalaryCalculationConfig Config() => new()
    {
        InstitutionId = Guid.NewGuid(),
        MinimumWage = MinimumWage,
        EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        // Oranlar varsayılan: 3308 Madde 25 ve Geçici Madde 12 ile birebir
        // (LargeBusinessRate 0.30 · SmallBusinessRate 0.15 · MEM12thGradeRate 0.50 ·
        //  GovContribLargeNonMEM 1/3 · GovContribSmallNonMEM 2/3 · GovContribMEM 1.0)
    };

    private static SalaryCalculator.Result Calculate(
        int personnelCount = 25,
        string educationType = "Anadolu",
        int classYear = 11,
        bool hasJourneymanQualification = false,
        int deductibleAbsenceDays = 0,
        bool isPublicInstitution = false)
        => SalaryCalculator.Calculate(
            Config(),
            personnelCount,
            educationType,
            classYear,
            hasJourneymanQualification,
            deductibleAbsenceDays,
            isPublicInstitution: isPublicInstitution);

    // ── Kamu kurumu ────────────────────────────────────────────────────────────────

    [Fact]
    public void Kamu_kurumunda_devlet_katkisi_odenmez()
    {
        // #157'nin çekirdeği: eskiden burada özel işletme oranı hesaplanıyordu.
        var result = Calculate(isPublicInstitution: true);

        result.GovernmentContribution.ShouldBe(0m);
        result.ContributionType.ShouldBe(GovernmentContributionType.PublicInstitution);
    }

    [Fact]
    public void Kamu_kurumunda_ogrencinin_UCRETI_etkilenmez()
    {
        // Kanun yalnız DEVLET KATKISINI kaldırır. Öğrencinin ücreti işletme tarafından
        // ödenmeye devam eder (Madde 25) — katkının sıfırlanması ücreti düşürmemeli.
        var publicResult = Calculate(isPublicInstitution: true);
        var privateResult = Calculate(isPublicInstitution: false);

        publicResult.BaseWage.ShouldBe(privateResult.BaseWage);
        publicResult.NetAmount.ShouldBe(privateResult.NetAmount);
        publicResult.Deduction.ShouldBe(privateResult.Deduction);
    }

    [Fact]
    public void Kamu_kontrolu_MESEM_oranindan_ONCE_gelir()
    {
        // MESEM öğrencisi normalde en yüksek katkıyı alır (en az ücretin TAMAMI). Kamu
        // kurumunda bu hiç hesaplanmamalı; kontrol sırası bu yüzden önemli.
        var result = Calculate(
            educationType: "Mesem",
            classYear: 12,
            hasJourneymanQualification: true,
            isPublicInstitution: true);

        result.GovernmentContribution.ShouldBe(0m);
        result.ContributionType.ShouldBe(GovernmentContributionType.PublicInstitution);
    }

    [Fact]
    public void Kamu_kurumunda_isletme_buyuklugu_katkiyi_degistirmez()
    {
        Calculate(personnelCount: 5, isPublicInstitution: true)
            .GovernmentContribution.ShouldBe(0m);

        Calculate(personnelCount: 500, isPublicInstitution: true)
            .GovernmentContribution.ShouldBe(0m);
    }

    // ── Özel işletme: mevcut davranış korunuyor (regresyon) ────────────────────────

    [Fact]
    public void Ozel_buyuk_isletme_ucte_bir_katki_alir()
    {
        // Geçici Madde 12: 20 ve üzeri personel → en az ücretin ÜÇTE BİRİ.
        // Taban = 20.000 × 0,30 = 6.000 → katkı = 6.000 × 1/3 = 2.000
        var result = Calculate(personnelCount: 25);

        result.ContributionType.ShouldBe(GovernmentContributionType.NonMemLarge);
        result.GovernmentContribution.ShouldBe(2_000m, tolerance: 0.01m);
    }

    [Fact]
    public void Ozel_kucuk_isletme_ucte_iki_katki_alir()
    {
        // Geçici Madde 12: 20'den az personel → en az ücretin ÜÇTE İKİSİ.
        // Taban = 20.000 × 0,15 = 3.000 → katkı = 3.000 × 2/3 = 2.000
        var result = Calculate(personnelCount: 5);

        result.ContributionType.ShouldBe(GovernmentContributionType.NonMemSmall);
        result.GovernmentContribution.ShouldBe(2_000m, tolerance: 0.01m);
    }

    [Fact]
    public void MESEM_ogrencisi_katkinin_TAMAMINI_alir()
    {
        // Geçici Madde 12: MESEM programına devam eden öğrenciye en az ücretin TAMAMI.
        // Taban = 20.000 × 0,50 = 10.000 → katkı = 10.000 (net tavanına eşit)
        var result = Calculate(
            educationType: "Mesem", classYear: 12, hasJourneymanQualification: true);

        result.ContributionType.ShouldBe(GovernmentContributionType.MemStudent);
        result.GovernmentContribution.ShouldBe(10_000m, tolerance: 0.01m);
    }

    [Fact]
    public void Katki_tipi_her_zaman_doldurulur()
    {
        // Tip alanı #157 ile eklendi: karar artık denetlenebilir. Hiçbir yolda boş kalmamalı.
        Calculate().ContributionType.ShouldNotBeNull();
        Calculate(isPublicInstitution: true).ContributionType.ShouldNotBeNull();
        Calculate(educationType: "Mesem", classYear: 12, hasJourneymanQualification: true)
            .ContributionType.ShouldNotBeNull();
    }
}
