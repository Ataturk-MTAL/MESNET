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

    [Fact]
    public void lists_all_13_districts_of_mersin()
    {
        TurkishDistricts.For(Mersin).Count.ShouldBe(13);
    }

    [Fact]
    public void districts_are_sorted_alphabetically_by_turkish_collation()
    {
        // tr-TR sırası ASCII'den farklı: Ç, C'den SONRA gelir (Bozyazı → Çamlıyayla → Erdemli).
        // Ordinal sıralamada 'Ç' (U+00C7) tüm ASCII harflerden sonra gelir ve liste yanlış görünür.
        var turkish = StringComparer.Create(new CultureInfo("tr-TR"), ignoreCase: false);
        var districts = TurkishDistricts.For(Mersin);

        districts.ShouldBe([.. districts.OrderBy(d => d, turkish)]);
    }

    [Fact]
    public void district_names_use_turkish_characters()
    {
        var districts = TurkishDistricts.For(Mersin);

        districts.ShouldContain("Aydıncık");
        districts.ShouldContain("Çamlıyayla");
        districts.ShouldContain("Yenişehir");
    }

    [Fact]
    public void district_names_are_unique()
    {
        var districts = TurkishDistricts.For(Mersin);

        districts.Distinct().Count().ShouldBe(districts.Count);
    }

    [Fact]
    public void every_listed_province_code_is_a_real_province()
    {
        // Liste elle doldurulduğu için var olmayan bir il koduna ilçe yazılması mümkün;
        // o ilçeler hiçbir zaman seçilemez hâle gelir ve sessizce ölü kalırdı.
        foreach (var province in TurkishProvinces.All)
        {
            if (TurkishDistricts.IsKnown(province.Key))
                TurkishProvinces.IsValidCode(province.Key).ShouldBeTrue();
        }

        TurkishDistricts.IsKnown(Mersin).ShouldBeTrue();
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
    [InlineData("06")]   // Ankara — gerçek il, listesi henüz doldurulmadı
    [InlineData("34")]
    [InlineData("82")]   // hiç yok
    [InlineData(null)]
    public void reports_provinces_without_a_district_list_as_unknown(string? provinceCode)
    {
        TurkishDistricts.IsKnown(provinceCode).ShouldBeFalse();
        TurkishDistricts.For(provinceCode).ShouldBeEmpty();
        TurkishDistricts.IsValid(provinceCode, "Çankaya").ShouldBeFalse();
    }
}
