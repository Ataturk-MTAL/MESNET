using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Kurum koordinasyon yapılandırmasının doğrulaması (#134).
///
/// <para>Kusur: <c>UpsertCoordinationConfigHandler</c> gelen mesafe-saat tablosunu ve azami
/// haftalık ek ders saatini hiç denetlemeden yazıyordu. Boş tablo, 0/negatif mesafe sınırı,
/// negatif saat, yinelenen sınır — hepsi kabul ediliyordu. Yapılandırma kurum genelidir:
/// bozuk tablo tüm alanların <c>MaxCoordinationHours</c> tavanlarını kaydırır, hatta boş
/// tablo <c>CalculateMaxHours</c>'ı çalışamaz hâle getirirdi.</para>
///
/// <para><b>Sıralama kural değildir</b> — okuma anında tablo zaten sıralanır; bu yüzden
/// "artan sırada gönder" şartı bilinçli olarak yoktur.</para>
/// </summary>
public sealed class CoordinationConfigPolicyTests
{
    /// <summary>Mevzuat tablosu — geçerli bir yapılandırmanın referansı.</summary>
    private static List<DistanceHourRule> ValidRules() =>
    [
        new(1.0, 2),
        new(3.0, 4),
        new(5.0, 6),
        new(double.MaxValue, 8),
    ];

    // ── Geçerli yapılandırma ──

    [Fact]
    public void Mevzuat_tablosu_ve_gecerli_azami_saat_kabul_edilir()
    {
        // Arrange
        var rules = ValidRules();

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: 20);

        // Assert
        violation.ShouldBeNull();
    }

    [Fact]
    public void Kurallar_artan_sirada_gonderilmek_zorunda_degildir()
    {
        // Arrange — aynı tablo karışık sırada; okuma anında zaten sıralanıyor
        List<DistanceHourRule> rules =
        [
            new(double.MaxValue, 8),
            new(3.0, 4),
            new(1.0, 2),
            new(5.0, 6),
        ];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: null);

        // Assert
        violation.ShouldBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(40)]
    public void Azami_saat_sinir_degerleri_dahil_kabul_edilir(int hours)
    {
        // Act
        var violation = CoordinationConfigPolicy.Validate(distanceHourRules: null, hours);

        // Assert
        violation.ShouldBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(40)]
    public void Kural_saati_sinir_degerleri_dahil_kabul_edilir(int hours)
    {
        // Arrange
        List<DistanceHourRule> rules = [new(double.MaxValue, hours)];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: null);

        // Assert
        violation.ShouldBeNull();
    }

    // ── Kısmi güncelleme: null alanlar denetlenmez ──

    [Fact]
    public void Iki_alan_da_null_ise_hicbir_kural_isletilmez()
    {
        // Arrange — yalnız IsMetropolitan güncelleniyor olabilir; o alanın kuralı yok

        // Act
        var violation = CoordinationConfigPolicy.Validate(
            distanceHourRules: null, maxWeeklyExtraHours: null);

        // Assert
        violation.ShouldBeNull();
    }

    [Fact]
    public void Tablo_null_ise_gecersiz_azami_saat_yine_de_yakalanir()
    {
        // Arrange — kısmi güncelleme denetimi zayıflatmaz

        // Act
        var violation = CoordinationConfigPolicy.Validate(distanceHourRules: null, maxWeeklyExtraHours: 0);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.InvalidMaxWeeklyExtraHours);
    }

    [Fact]
    public void Azami_saat_null_ise_gecersiz_tablo_yine_de_yakalanir()
    {
        // Arrange
        List<DistanceHourRule> rules = [];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: null);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.EmptyDistanceHourRules);
    }

    // ── Tablo kuralları: reddedilme ──

    [Fact]
    public void Bos_tablo_reddedilir()
    {
        // Arrange — tablo gönderildi ama içi boş: hiçbir mesafe saate eşlenemez
        List<DistanceHourRule> rules = [];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: 20);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.EmptyDistanceHourRules);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Pozitif_olmayan_mesafe_siniri_reddedilir(double maxDistanceKm)
    {
        // Arrange
        List<DistanceHourRule> rules = [new(maxDistanceKm, 2), new(double.MaxValue, 8)];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: 20);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.InvalidDistanceHourRuleDistance);
        violation.DistanceKm.ShouldBe(maxDistanceKm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(41)]
    public void Aralik_disi_kural_saati_reddedilir(int hours)
    {
        // Arrange
        List<DistanceHourRule> rules = [new(1.0, hours), new(double.MaxValue, 8)];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: 20);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.InvalidDistanceHourRuleHours);
        violation.Hours.ShouldBe(hours);
        violation.DistanceKm.ShouldBe(1.0);
    }

    [Fact]
    public void Ayni_mesafe_siniri_iki_kez_gecemez()
    {
        // Arrange — hangi saatin uygulanacağı sıraya kalırdı, oysa sıra anlamsız
        List<DistanceHourRule> rules = [new(3.0, 4), new(3.0, 6), new(double.MaxValue, 8)];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: 20);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.DuplicateDistanceHourRule);
        violation.DistanceKm.ShouldBe(3.0);
    }

    [Fact]
    public void Sinirsiz_catch_all_kurali_olmayan_tablo_reddedilir()
    {
        // Arrange — 5 km'nin ötesindeki işletmeler hiçbir kurala girmezdi
        List<DistanceHourRule> rules = [new(1.0, 2), new(3.0, 4), new(5.0, 6)];

        // Act
        var violation = CoordinationConfigPolicy.Validate(rules, maxWeeklyExtraHours: 20);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.MissingUnlimitedDistanceHourRule);
    }

    [Fact]
    public void Varsayilan_yapilandirma_kendi_dogrulamasindan_gecer()
    {
        // Arrange — CoordinationConfig varsayılanı üretim yolunda fallback olarak kullanılıyor
        var config = new CoordinationConfig();

        // Act
        var violation = CoordinationConfigPolicy.Validate(
            config.DistanceHourRules, config.MaxWeeklyExtraHours);

        // Assert
        violation.ShouldBeNull();
    }

    // ── Azami haftalık ek ders saati: reddedilme ──

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(41)]
    public void Aralik_disi_azami_haftalik_ek_ders_saati_reddedilir(int hours)
    {
        // Act
        var violation = CoordinationConfigPolicy.Validate(ValidRules(), hours);

        // Assert
        violation!.Kind.ShouldBe(CoordinationConfigViolationKind.InvalidMaxWeeklyExtraHours);
        violation.Hours.ShouldBe(hours);
    }

    // ── Hata mesajı eşlemesi ──

    [Fact]
    public void Her_ihlal_turu_kendi_hata_koduna_eslenir()
    {
        // Arrange — kod ailesi Coordination.{Kind}; frontend tek eşleme tablosu tutar
        foreach (var kind in CoordinationConfigViolationKind.List)
        {
            // Act
            var error = CoordinationErrors.CoordinationConfigInvalid(
                new CoordinationConfigViolation(kind, DistanceKm: 3.0, Hours: 41));

            // Assert
            error.Code.ShouldBe($"Coordination.{kind.Name}");
            error.Description.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Sinirsiz_kuralin_mesafesi_kullaniciya_sayi_olarak_gosterilmez()
    {
        // Arrange — double.MaxValue ham hâliyle 1,79E+308 olarak okunurdu

        // Act
        var error = CoordinationErrors.DuplicateDistanceHourRule(double.MaxValue);

        // Assert
        error.Description.ShouldContain("sınırsız");
        error.Description.ShouldNotContain("E+308");
    }
}
