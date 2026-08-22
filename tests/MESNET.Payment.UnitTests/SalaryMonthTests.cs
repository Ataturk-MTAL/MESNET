using MESNET.Payment.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Asgari ücret yıl içinde birden fazla kez artabildiği için hesap, ayda yürürlükte olan
/// config'i seçmek zorundadır — hesabın koştuğu andaki config'i değil.
/// </summary>
public class SalaryMonthTests
{
    private static readonly DateTime Fallback = new(2027, 3, 15, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void returns_last_day_of_the_given_month_at_midnight_utc()
    {
        // Act
        var result = SalaryMonth.ConfigReferenceDate("2026-12", Fallback);

        // Assert
        result.ShouldBe(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        result.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void handles_february_in_a_leap_year()
    {
        SalaryMonth.ConfigReferenceDate("2028-02", Fallback)
            .ShouldBe(new DateTime(2028, 2, 29, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void handles_february_in_a_common_year()
    {
        SalaryMonth.ConfigReferenceDate("2027-02", Fallback)
            .ShouldBe(new DateTime(2027, 2, 28, 0, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Yürürlük zinciri eski kaydı <c>EffectiveFrom.AddDays(-1)</c> ile kapatır, yani
    /// <c>EffectiveTo</c> o günün BAŞINDADIR. Referans tarihi gün sonu (23:59) olsaydı
    /// <c>EffectiveTo &gt;= referenceDate</c> koşulu kendi kapanış gününde tutmaz ve hiç config
    /// bulunmazdı (<c>SalaryConfigMissing</c> → HTTP 422).
    /// </summary>
    [Fact]
    public void reference_date_stays_within_the_closing_boundary_of_the_previous_config()
    {
        // Arrange — 01.01.2027 zammı: önceki kayıt 31.12.2026 00:00'da kapanır
        var newWageEffectiveFrom = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousEffectiveTo = newWageEffectiveFrom.AddDays(-1);

        // Act
        var december = SalaryMonth.ConfigReferenceDate("2026-12", Fallback);

        // Assert
        (previousEffectiveTo >= december).ShouldBeTrue("Aralık hesabı kapanmış config'i bulamıyor.");
        (newWageEffectiveFrom > december).ShouldBeTrue("Aralık hesabı 2027 ücretini seçiyor.");
    }

    [Fact]
    public void selects_the_new_wage_for_the_month_the_raise_takes_effect_in()
    {
        // Arrange
        var newWageEffectiveFrom = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var january = SalaryMonth.ConfigReferenceDate("2027-01", Fallback);

        // Assert
        (newWageEffectiveFrom <= january).ShouldBeTrue();
    }

    /// <summary>
    /// Yılda üç zam senaryosu: 01.01, 01.07 ve 01.10 yürürlükleri birbirini kapatır; her ay
    /// kendi döneminin tutarını seçer.
    /// </summary>
    [Theory]
    [InlineData("2027-01", 1)]
    [InlineData("2027-06", 1)]
    [InlineData("2027-07", 2)]
    [InlineData("2027-09", 2)]
    [InlineData("2027-10", 3)]
    [InlineData("2027-12", 3)]
    public void picks_the_config_in_force_for_each_month_across_three_raises(
        string month, int expectedConfigIndex)
    {
        // Arrange — zincir: [01.01 → 30.06], [01.07 → 30.09], [01.10 → açık]
        var starts = new[]
        {
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var chain = starts
            .Select((from, i) => (
                Index: i + 1,
                From: from,
                To: i + 1 < starts.Length ? starts[i + 1].AddDays(-1) : (DateTime?)null))
            .ToList();

        // Act — PaymentSaga.CalculateAsync ile aynı seçim mantığı
        var configDate = SalaryMonth.ConfigReferenceDate(month, Fallback);
        var selected = chain
            .Where(c => c.From <= configDate && (c.To is null || c.To >= configDate))
            .OrderByDescending(c => c.From)
            .First();

        // Assert
        selected.Index.ShouldBe(expectedConfigIndex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2026")]
    [InlineData("2026-13")]
    [InlineData("2026-00")]
    [InlineData("Aralık 2026")]
    [InlineData("2026-12-31")]
    public void falls_back_when_the_month_cannot_be_parsed(string? month)
    {
        // Act
        var result = SalaryMonth.ConfigReferenceDate(month, Fallback);

        // Assert — çözümlenemeyen ay hesabı durdurmaz, eski davranışa döner
        result.ShouldBe(Fallback);
    }
}
