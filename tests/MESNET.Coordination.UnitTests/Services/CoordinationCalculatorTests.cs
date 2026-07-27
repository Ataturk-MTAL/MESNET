using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.Entities;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Mesafe → verilebilecek koordinatörlük saati hesabı (#134).
///
/// <para>İki gerçek kusur kilitleniyor:</para>
/// <list type="number">
///   <item>Fallback <c>rules.Last()</c> <b>sıralanmamış</b> listenin sonuncusunu alıyordu;
///   eşleşme döngüsü ise sıralanmış liste üzerinde yürüyordu. Tablo
///   <c>[(5.0, 6), (3.0, 4)]</c> sırasıyla saklandıysa fallback yanlış kuralı döndürüyordu.</item>
///   <item>Boş tabloda <c>rules.Last()</c> <c>InvalidOperationException</c> fırlatıyordu —
///   çağıranların hiçbiri bunu beklemiyordu.</item>
/// </list>
/// </summary>
public sealed class CoordinationCalculatorTests
{
    /// <summary>Mevzuat tablosu: ≤1km→2s, ≤3km→4s, ≤5km→6s, >5km→8s.</summary>
    private static List<DistanceHourRule> RegulationRules() =>
    [
        new(1.0, 2),
        new(3.0, 4),
        new(5.0, 6),
        new(double.MaxValue, 8),
    ];

    // ── Mevzuat tablosu ──

    [Theory]
    [InlineData(0.5, 2)]
    [InlineData(1.0, 2)]
    [InlineData(2.0, 4)]
    [InlineData(3.0, 4)]
    [InlineData(5.0, 6)]
    [InlineData(10.0, 8)]
    public void Mevzuat_tablosu_mesafeyi_dogru_saate_esler(double distanceKm, int expectedHours)
    {
        // Arrange
        var rules = RegulationRules();

        // Act
        var hours = CoordinationCalculator.CalculateMaxHours(distanceKm, rules);

        // Assert
        hours.ShouldBe(expectedHours);
    }

    // ── Saklanma sırasından bağımsızlık ──

    [Theory]
    [InlineData(0.5, 2)]
    [InlineData(2.0, 4)]
    [InlineData(5.0, 6)]
    [InlineData(10.0, 8)]
    public void Siralanmamis_tablo_ayni_sonucu_verir(double distanceKm, int expectedHours)
    {
        // Arrange — aynı kurallar azalan/karışık sırada saklanmış
        List<DistanceHourRule> rules =
        [
            new(double.MaxValue, 8),
            new(5.0, 6),
            new(1.0, 2),
            new(3.0, 4),
        ];

        // Act
        var hours = CoordinationCalculator.CalculateMaxHours(distanceKm, rules);

        // Assert
        hours.ShouldBe(expectedHours);
    }

    // ── Fallback (catch-all kuralı olmayan tablo) ──

    [Fact]
    public void Catch_all_yoksa_fallback_siralanmis_sonuncuyu_kullanir()
    {
        // Arrange — kurallar azalan sırada saklanmış; en büyük sınır 5.0 km → 6 saat.
        // Eski kod sıralanmamış listenin sonuncusunu (3.0 km → 4 saat) döndürüyordu.
        List<DistanceHourRule> rules = [new(5.0, 6), new(3.0, 4)];

        // Act
        var hours = CoordinationCalculator.CalculateMaxHours(distanceKm: 42.0, rules);

        // Assert
        hours.ShouldBe(6);
    }

    [Fact]
    public void Catch_all_yoksa_fallback_zaten_sirali_tabloda_da_sonuncuyu_kullanir()
    {
        // Arrange — aynı kurallar artan sırada; sonuç saklanma sırasından bağımsız olmalı
        List<DistanceHourRule> rules = [new(3.0, 4), new(5.0, 6)];

        // Act
        var hours = CoordinationCalculator.CalculateMaxHours(distanceKm: 42.0, rules);

        // Assert
        hours.ShouldBe(6);
    }

    // ── Savunmacı davranış ──

    [Fact]
    public void Bos_tablo_anlamli_istisna_firlatir()
    {
        // Arrange — yapılandırma ucu artık boş tabloyu reddediyor (#134), ama hesap
        // kendini savunmalı: sessiz yanlış saat yerine açık hata.
        var rules = new List<DistanceHourRule>();

        // Act
        var act = () => { CoordinationCalculator.CalculateMaxHours(distanceKm: 1.0, rules); };

        // Assert
        var exception = Should.Throw<ArgumentException>(act);
        exception.ParamName.ShouldBe("rules");
    }

    [Fact]
    public void Null_tablo_anlamli_istisna_firlatir()
    {
        // Act
        var act = () => { CoordinationCalculator.CalculateMaxHours(distanceKm: 1.0, rules: null!); };

        // Assert
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("rules");
    }
}
