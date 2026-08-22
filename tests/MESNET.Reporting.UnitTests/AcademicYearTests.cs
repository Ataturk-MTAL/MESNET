using MESNET.Common.Shared;
using Shouldly;
using Xunit;

namespace MESNET.Reporting.UnitTests;

/// <summary>
/// Eğitim yılı metninin tek kanonik biçimi "2025-2026" (#112).
/// Testler Common.Shared'ı hedefler; solution'da Common.Shared'a ait ayrı bir birim test
/// projesi bulunmadığı için mevcut Reporting birim test projesine eklendi — helper'ın
/// birincil tüketicisi Reporting modülüdür (belge üretimi + liste eşlemesi).
/// </summary>
public class AcademicYearTests
{
    [Fact]
    public void Format_baslangic_ve_bitis_yilini_tire_ile_birlestirir()
    {
        // Arrange & Act
        var result = AcademicYear.Format(2025, 2026);

        // Assert
        result.ShouldBe("2025-2026");
    }

    [Theory]
    [InlineData("2025 / 2026", "2025-2026")]   // eski frontend + scheduler biçimi
    [InlineData("2025/2026", "2025-2026")]
    [InlineData("2025 - 2026", "2025-2026")]
    [InlineData("2025 – 2026", "2025-2026")]   // en tire (–)
    [InlineData("2025-2026", "2025-2026")]     // zaten kanonik — değişmez
    [InlineData("  2025 / 2026  ", "2025-2026")]
    public void Normalize_bilinen_ayraclari_kanonik_tireye_cevirir(string raw, string expected)
    {
        AcademicYear.Normalize(raw).ShouldBe(expected);
    }

    [Theory]
    [InlineData("2025", "2025")]               // tek yıl — dokunulmaz
    [InlineData("Öğretim Yılı", "Öğretim Yılı")]
    [InlineData("25/26", "25/26")]             // 4 haneli değil — eşleşmez
    public void Normalize_eslesmeyen_metni_oldugu_gibi_birakir(string raw, string expected)
    {
        AcademicYear.Normalize(raw).ShouldBe(expected);
    }

    [Fact]
    public void Normalize_null_ve_bos_girdiyi_oldugu_gibi_dondurur()
    {
        AcademicYear.Normalize(null).ShouldBeNull();
        AcademicYear.Normalize("").ShouldBe("");
        AcademicYear.Normalize("   ").ShouldBe("   ");
    }
}
