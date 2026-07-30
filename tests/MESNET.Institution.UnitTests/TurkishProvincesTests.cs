using MESNET.Common.Shared.Reference;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// İl kodu kapsam kararının anahtarıdır (#147) — liste eksik ya da biçimi kaymışsa kurum
/// kaydedilemez ya da yanlış ilde kaydedilir.
/// </summary>
public class TurkishProvincesTests
{
    [Fact]
    public void contains_all_81_provinces()
    {
        TurkishProvinces.All.Count.ShouldBe(81);
    }

    [Fact]
    public void codes_are_zero_padded_two_digit_strings_from_01_to_81()
    {
        // Baştaki sıfır anlamlı: "01" int'e çevrilse "1" olur ve iki farklı değer doğar.
        var expected = Enumerable.Range(1, 81).Select(i => i.ToString("00")).ToList();

        TurkishProvinces.All.Select(p => p.Key).ShouldBe(expected);
    }

    [Fact]
    public void province_names_are_unique()
    {
        var names = TurkishProvinces.All.Select(p => p.Value).ToList();

        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Theory]
    [InlineData("01", "Adana")]
    [InlineData("06", "Ankara")]
    // Seeder'daki kurum Mersin'de; bu eşleşme bozulursa seed 422 ile durur.
    [InlineData("33", "Mersin")]
    [InlineData("34", "İstanbul")]
    [InlineData("46", "Kahramanmaraş")]
    [InlineData("81", "Düzce")]
    public void resolves_known_codes_to_their_names(string code, string expectedName)
    {
        TurkishProvinces.GetName(code).ShouldBe(expectedName);
        TurkishProvinces.IsValidCode(code).ShouldBeTrue();
    }

    [Fact]
    public void province_names_use_turkish_characters_not_ascii_approximations()
    {
        TurkishProvinces.GetName("40").ShouldBe("Kırşehir");
        TurkishProvinces.GetName("63").ShouldBe("Şanlıurfa");
        TurkishProvinces.GetName("29").ShouldBe("Gümüşhane");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("00")]
    [InlineData("82")]
    [InlineData("1")]      // sıfır dolgusuz — aynı ilin ikinci gösterimi olur, kabul edilmez
    [InlineData("033")]
    [InlineData("33 ")]    // sonda boşluk — serbest metin sızıntısının klasik hâli
    [InlineData("Mersin")] // il ADI kod alanına yazılamaz (#147'nin tam olarak engellediği şey)
    [InlineData("mersin")]
    public void rejects_anything_that_is_not_an_exact_known_code(string? code)
    {
        TurkishProvinces.IsValidCode(code).ShouldBeFalse();
        TurkishProvinces.GetName(code).ShouldBeNull();
    }
}
