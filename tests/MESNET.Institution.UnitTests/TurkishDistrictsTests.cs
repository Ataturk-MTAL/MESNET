using System.Globalization;
using MESNET.Common.Shared.Reference;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// İlçe listesi kapalıdır: listede olmayan ad reddedilir. Sıra da davranışın parçasıdır —
/// açılır liste alfabetik gösterilir ve sıra sunucudan gelir.
/// </summary>
public class TurkishDistrictsTests
{
    private const string Mersin = "33";

    private static readonly StringComparer Turkish =
        StringComparer.Create(new CultureInfo("tr-TR"), ignoreCase: false);

    [Fact]
    public void covers_all_81_provinces()
    {
        TurkishProvinces.All.Count(p => TurkishDistricts.IsKnown(p.Key)).ShouldBe(81);
    }

    [Fact]
    public void contains_973_districts_in_total()
    {
        // Türkiye'deki ilçe sayısı. Sapma, bir ilin listesinin eksik ya da mükerrer
        // girildiğini gösterir — tek tek gözle bulunamayacak bir hata.
        TurkishProvinces.All.Sum(p => TurkishDistricts.For(p.Key).Count).ShouldBe(973);
    }

    [Fact]
    public void lists_all_13_districts_of_mersin()
    {
        TurkishDistricts.For(Mersin).Count.ShouldBe(13);
    }

    [Fact]
    public void every_province_has_at_least_one_district()
    {
        foreach (var province in TurkishProvinces.All)
            TurkishDistricts.For(province.Key).ShouldNotBeEmpty(province.Value);
    }

    [Fact]
    public void districts_are_sorted_alphabetically_by_turkish_collation_in_every_province()
    {
        // tr-TR sırası ASCII'den farklı: Ç, C'den SONRA gelir (Bozyazı → Çamlıyayla → Erdemli).
        // Ordinal sıralamada 'Ç' (U+00C7) tüm ASCII harflerden sonra gelir ve liste yanlış
        // görünür. Sıra davranışın parçası: açılır liste sunucudan geldiği gibi gösterilir.
        foreach (var province in TurkishProvinces.All)
        {
            var districts = TurkishDistricts.For(province.Key);
            districts.ShouldBe([.. districts.OrderBy(d => d, Turkish)], province.Value);
        }
    }

    [Fact]
    public void district_names_are_unique_within_every_province()
    {
        foreach (var province in TurkishProvinces.All)
        {
            var districts = TurkishDistricts.For(province.Key);
            districts.Distinct().Count().ShouldBe(districts.Count, province.Value);
        }
    }

    [Fact]
    public void district_names_are_trimmed_and_non_empty()
    {
        foreach (var province in TurkishProvinces.All)
        foreach (var district in TurkishDistricts.For(province.Key))
        {
            district.ShouldNotBeNullOrWhiteSpace();
            district.ShouldBe(district.Trim(), $"{province.Value}: \"{district}\"");
        }
    }

    [Fact]
    public void district_names_use_turkish_characters()
    {
        var districts = TurkishDistricts.For(Mersin);

        districts.ShouldContain("Aydıncık");
        districts.ShouldContain("Çamlıyayla");
        districts.ShouldContain("Yenişehir");
    }

    [Theory]
    [InlineData("Toroslar")]
    [InlineData("Akdeniz")]
    [InlineData("Çamlıyayla")]
    public void accepts_a_district_written_exactly_as_listed(string districtName)
    {
        TurkishDistricts.IsValid(Mersin, districtName).ShouldBeTrue();
    }

    [Theory]
    [InlineData("toroslar")]
    [InlineData("TOROSLAR")]
    [InlineData("Toroslar ")]
    [InlineData(" Toroslar")]
    [InlineData("Torroslar")]
    [InlineData("")]
    [InlineData(null)]
    public void rejects_anything_that_is_not_written_exactly_as_listed(string? districtName)
    {
        // Esneklik BİLEREK yok: "toroslar" kabul edilseydi aynı ilçe iki farklı değerle
        // kaydolur ve #147'nin serbest metne itirazı ilçe tarafında geri gelirdi.
        TurkishDistricts.IsValid(Mersin, districtName).ShouldBeFalse();
    }

    [Fact]
    public void rejects_a_district_from_another_province()
    {
        TurkishDistricts.IsValid(Mersin, "Çankaya").ShouldBeFalse();
    }

    [Theory]
    [InlineData("82")]   // 81'den büyük — il yok
    [InlineData("00")]
    [InlineData("1")]    // sıfır dolgusuz
    [InlineData(null)]
    public void reports_an_unknown_province_code_as_having_no_districts(string? provinceCode)
    {
        TurkishDistricts.IsKnown(provinceCode).ShouldBeFalse();
        TurkishDistricts.For(provinceCode).ShouldBeEmpty();
        TurkishDistricts.IsValid(provinceCode, "Çankaya").ShouldBeFalse();
    }

    [Fact]
    public void metropolitan_provinces_have_no_merkez_district()
    {
        // Büyükşehirde merkez ilçe bölünmüştür; "Merkez" kaydı olması veri hatası olurdu.
        TurkishDistricts.For("34").ShouldNotContain("Merkez");   // İstanbul
        TurkishDistricts.For("06").ShouldNotContain("Merkez");   // Ankara
        TurkishDistricts.For(Mersin).ShouldNotContain("Merkez");
    }

    [Fact]
    public void non_metropolitan_provinces_keep_their_merkez_district()
    {
        TurkishDistricts.For("69").ShouldContain("Merkez");   // Bayburt
        TurkishDistricts.For("79").ShouldContain("Merkez");   // Kilis
    }
}
