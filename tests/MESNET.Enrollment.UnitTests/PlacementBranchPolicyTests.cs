using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Policies;
using MESNET.Enrollment.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>Yerleştirme alan yetkisi guard'ı ve geçiş dolgusu girdisi (#119).</summary>
public class PlacementBranchPolicyTests
{
    private static BusinessBranchAuthorizationView View(params string[] codes) => new()
    {
        Id = Guid.NewGuid(),
        BusinessName = "Mezitli Elektrik Sanayi",
        ActiveBranchCodes = [.. codes]
    };

    [Fact]
    public void Isletme_yetkili_alandan_ogrenci_alabilir()
    {
        // Arrange
        var view = View("EET", "MTT");

        // Act
        var authorized = PlacementBranchPolicy.IsBusinessAuthorizedFor(view, "EET");

        // Assert
        authorized.ShouldBeTrue();
    }

    [Fact]
    public void Yetkisiz_alandan_yerlestirme_reddedilir()
    {
        // Arrange
        var view = View("EET");

        // Act
        var authorized = PlacementBranchPolicy.IsBusinessAuthorizedFor(view, "BT");

        // Assert
        authorized.ShouldBeFalse();
    }

    [Fact]
    public void Yetki_kaydi_hic_yoksa_yerlestirme_reddedilir()
    {
        // Arrange & Act
        var authorized = PlacementBranchPolicy.IsBusinessAuthorizedFor(null, "EET");

        // Assert
        authorized.ShouldBeFalse();
    }

    [Fact]
    public void Bos_yetki_listesi_kapali_demektir()
    {
        // Arrange
        var view = View();

        // Act
        var authorized = PlacementBranchPolicy.IsBusinessAuthorizedFor(view, "EET");

        // Assert
        authorized.ShouldBeFalse();
    }

    [Fact]
    public void Alan_kodu_bosluklu_veya_farkli_buyuk_kucuk_harfle_gelse_de_eslesir()
    {
        // Arrange
        var view = View("EET");

        // Act & Assert
        PlacementBranchPolicy.IsBusinessAuthorizedFor(view, " eet ").ShouldBeTrue();
        PlacementBranchPolicy.IsBusinessAuthorizedFor(view, "").ShouldBeFalse();
        PlacementBranchPolicy.IsBusinessAuthorizedFor(view, null).ShouldBeFalse();
    }

    [Fact]
    public void Dolgu_isletme_basina_farkli_alan_kodlarini_gruplar()
    {
        // Arrange
        var businessA = Guid.NewGuid();
        var businessB = Guid.NewGuid();
        var placements = new List<InternshipPlacement>
        {
            new() { Id = Guid.NewGuid(), BusinessId = businessA, BranchCode = "EET" },
            new() { Id = Guid.NewGuid(), BusinessId = businessA, BranchCode = "EET" },
            new() { Id = Guid.NewGuid(), BusinessId = businessA, BranchCode = "MTT" },
            new() { Id = Guid.NewGuid(), BusinessId = businessB, BranchCode = "BT" },
        };

        // Act
        var grouped = PlacementBranchPolicy.GroupBranchCodesByBusiness(placements);

        // Assert
        grouped.Count.ShouldBe(2);
        grouped[businessA].ShouldBe(["EET", "MTT"]);
        grouped[businessB].ShouldBe(["BT"]);
    }

    [Fact]
    public void Dolgu_alan_kodu_bos_olan_yerlestirmeleri_yok_sayar()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var placements = new List<InternshipPlacement>
        {
            new() { Id = Guid.NewGuid(), BusinessId = businessId, BranchCode = "" },
            new() { Id = Guid.NewGuid(), BusinessId = businessId, BranchCode = "   " },
        };

        // Act
        var grouped = PlacementBranchPolicy.GroupBranchCodesByBusiness(placements);

        // Assert
        grouped.ShouldBeEmpty();
    }
}
