using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Ay içi fesihte ücret ve teşvikin gün bazlı oranlanması (#154) — hesap tarafı.
///
/// <para><b>Hata neydi:</b> <c>SalaryCalculator</c> her zaman TAM AY varsayıyor, üzerinden
/// yalnız devamsızlık günü düşüyordu. "Kaç gün istihdam edildi" diye bir kavram yoktu.
/// Ay ortasında işletme değiştiren öğrencide ayrılınan işletmenin yükümlülüğü hiç
/// hesaplanamıyordu.</para>
///
/// <para><b>Kural:</b> her işletme, öğrencinin kendisinde çalıştığı gün sayısı oranında ücret
/// öder ve aynı oranda devlet katkısı alır. Gün sayımı <see cref="EmploymentDaysTests"/>.</para>
/// </summary>
public sealed class SalaryProrationTests
{
    private const decimal MinimumWage = 20_000m;

    private static SalaryCalculationConfig Config() => new()
    {
        MinimumWage = MinimumWage,
        EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        // Varsayılan oranlar: LargeBusinessRate 0,30 · GovContribLargeNonMEM 1/3
    };

    /// <summary>Büyük işletme, 11. sınıf, formal eğitim — taban 20.000 × 0,30 = 6.000.</summary>
    private static SalaryCalculator.Result Calculate(
        int employedDays = 30,
        int deductibleAbsenceDays = 0,
        decimal? agreedMonthlyWage = null)
        => SalaryCalculator.Calculate(
            Config(),
            personnelCount: 25,
            educationTypeName: "Anadolu",
            classYear: 11,
            hasJourneymanQualification: false,
            deductibleAbsenceDays: deductibleAbsenceDays,
            agreedMonthlyWage: agreedMonthlyWage,
            employedDays: employedDays);

    // ── Ücret oranlaması ───────────────────────────────────────────────────────────

    [Fact]
    public void Tam_ay_calisan_ogrenci_tam_taban_alir()
    {
        // Regresyon: oranlama eklenirken tam ay davranışı değişmemeli.
        Calculate().BaseWage.ShouldBe(6_000m, tolerance: 0.01m);
    }

    [Fact]
    public void Yarim_ay_calisan_ogrenci_yarim_taban_alir()
    {
        // 15 gün × (6.000 / 30) = 3.000
        var result = Calculate(employedDays: 15);

        result.BaseWage.ShouldBe(3_000m, tolerance: 0.01m);
        result.NetAmount.ShouldBe(3_000m, tolerance: 0.01m);
    }

    [Fact]
    public void Ay_ici_devirde_iki_isletmenin_toplami_gun_sayisiyla_orantilidir()
    {
        // 15'inde fesih (15 gün) + 16'sında yeni sözleşme (16 gün) = 31 gün.
        // Günlük ücret sabit 200 TL olduğu için toplam 6.200 TL — 31 günlük ayda
        // tabanı AŞAR ve bu bilinerek kabul edildi (#154). Kırpma yapılmaz.
        var ayrilan = Calculate(employedDays: 15);
        var yeni = Calculate(employedDays: 16);

        ayrilan.NetAmount.ShouldBe(3_000m, tolerance: 0.01m);
        yeni.NetAmount.ShouldBe(3_200m, tolerance: 0.01m);
        (ayrilan.NetAmount + yeni.NetAmount).ShouldBe(6_200m, tolerance: 0.01m);
    }

    [Fact]
    public void Hic_calisilmayan_ay_sifir_ucret_uretir()
    {
        var result = Calculate(employedDays: 0);

        result.BaseWage.ShouldBe(0m);
        result.NetAmount.ShouldBe(0m);
        result.GovernmentContribution.ShouldBe(0m);
    }

    [Fact]
    public void Sozlesmede_taahhut_edilen_yuksek_ucret_de_oranlanir()
    {
        // Taahhüt 9.000 (yasal taban 6.000'in üstünde, #84) → günlük 300 → 15 gün = 4.500
        Calculate(employedDays: 15, agreedMonthlyWage: 9_000m)
            .BaseWage.ShouldBe(4_500m, tolerance: 0.01m);
    }

    // ── Devamsızlık kesintisi oranlanmış tutar üzerinden ───────────────────────────

    [Fact]
    public void Devamsizlik_kesintisi_gunluk_ucretten_hesaplanir()
    {
        // Kesinti günlük ücrete bağlıdır, istihdam gününe değil: 3 gün × 200 = 600.
        // 15 gün çalışan → 3.000 - 600 = 2.400
        var result = Calculate(employedDays: 15, deductibleAbsenceDays: 3);

        result.Deduction.ShouldBe(600m, tolerance: 0.01m);
        result.NetAmount.ShouldBe(2_400m, tolerance: 0.01m);
    }

    [Fact]
    public void Kesinti_oranlanmis_tutari_asamaz()
    {
        // 10 gün çalışıp 20 gün devamsız olamaz, ama bozuk veri ücreti negatife düşürmemeli.
        // Eskiden tavan TAM AY tabanıydı; oranlamadan sonra tavan da oranlı olmalı.
        var result = Calculate(employedDays: 10, deductibleAbsenceDays: 25);

        result.Deduction.ShouldBe(2_000m, tolerance: 0.01m);   // 10 gün × 200
        result.NetAmount.ShouldBe(0m);
    }

    // ── Devlet katkısı aynı oranla ─────────────────────────────────────────────────

    [Fact]
    public void Devlet_katkisi_ucretle_ayni_gun_oraninda_hesaplanir()
    {
        // Tam ay katkısı: 6.000 × 1/3 = 2.000 → 15 günde yarısı = 1.000
        Calculate(employedDays: 15)
            .GovernmentContribution.ShouldBe(1_000m, tolerance: 0.01m);
    }

    [Fact]
    public void Devlet_katkisi_oranlanmis_neti_asamaz()
    {
        // Katkı fiilen ödenen ücreti aşamaz kuralı (mevcut) oranlamadan sonra da geçerli:
        // 3 gün çalışma → net 600; katkı ham hâliyle 200 olurdu, tavan devreye girmez.
        // Devamsızlıkla net düşürülünce tavan bağlar: 3 gün çalışma + 3 gün devamsızlık → net 0.
        var result = Calculate(employedDays: 3, deductibleAbsenceDays: 3);

        result.NetAmount.ShouldBe(0m);
        result.GovernmentContribution.ShouldBe(0m);
    }

    [Fact]
    public void Tam_ay_katkisi_oranlama_eklendikten_sonra_degismez()
    {
        // Regresyon: #157/#83/#84'te kilitlenen tam ay katkısı bozulmamalı.
        Calculate().GovernmentContribution.ShouldBe(2_000m, tolerance: 0.01m);
    }

    // ── Denetlenebilirlik ──────────────────────────────────────────────────────────

    [Fact]
    public void Sonuc_kac_gun_uzerinden_hesaplandigini_tasir()
    {
        // "Neden bu tutar" sorusu cevaplanabilir olmalı — ContributionType ile aynı gerekçe (#157).
        Calculate(employedDays: 15).EmployedDays.ShouldBe(15);
    }
}
