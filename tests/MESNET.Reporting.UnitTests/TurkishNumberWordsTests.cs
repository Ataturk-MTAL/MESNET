using MESNET.Reporting.Core.Utilities;
using Shouldly;
using Xunit;

namespace MESNET.Reporting.UnitTests;

/// <summary>
/// Dönem Not Fişi "Ort. (Yazı ile)" hücresinin kaynağı (#99).
/// KARAR (koordinatör): yalnız tam sayı kısmı yazıyla yazılır, ondalık atılır.
/// </summary>
public class TurkishNumberWordsTests
{
    [Theory]
    [InlineData(0, "Sıfır")]
    [InlineData(1, "Bir")]
    [InlineData(2, "İki")]      // Türkçe büyük harf: i → İ
    [InlineData(3, "Üç")]
    [InlineData(9, "Dokuz")]
    [InlineData(10, "On")]
    [InlineData(11, "On bir")]
    [InlineData(20, "Yirmi")]
    [InlineData(45, "Kırk beş")]
    [InlineData(67, "Altmış yedi")]
    [InlineData(81, "Seksen bir")]
    [InlineData(90, "Doksan")]
    [InlineData(99, "Doksan dokuz")]
    [InlineData(100, "Yüz")]
    public void ToWords_returns_turkish_words_for_score_range(int value, string expected)
    {
        // Act
        var result = TurkishNumberWords.ToWords(value);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(101, "Yüz bir")]
    [InlineData(115, "Yüz on beş")]
    [InlineData(200, "İki yüz")]
    [InlineData(999, "Dokuz yüz doksan dokuz")]
    public void ToWords_supports_three_digit_values(int value, string expected)
    {
        var result = TurkishNumberWords.ToWords(value);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public void ToWords_throws_when_value_is_outside_supported_range(int value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TurkishNumberWords.ToWords(value));
    }

    [Theory]
    [InlineData(81.67, "Seksen bir")]   // Issue #99'daki örnek
    [InlineData(81.00, "Seksen bir")]
    [InlineData(99.99, "Doksan dokuz")] // Yuvarlama YOK — ondalık atılır
    [InlineData(0.99, "Sıfır")]
    [InlineData(100.00, "Yüz")]
    // InlineData decimal sabit kabul etmez (attribute argümanı olamaz) — double alınıp çevrilir.
    public void FromScore_drops_the_decimal_part(double score, string expected)
    {
        // Act
        var result = TurkishNumberWords.FromScore((decimal)score);

        // Assert
        result.ShouldBe(expected);
    }
}
