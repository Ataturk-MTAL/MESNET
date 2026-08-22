using MESNET.Business.Core.Policies;
using MESNET.Business.Core.ValueObjects;
using Shouldly;
using Xunit;
using BusinessEntity = MESNET.Business.Core.Entities.Business;

namespace MESNET.Business.UnitTests;

/// <summary>
/// <c>Business.ActiveBranchCodes</c>, LINQ sorguları için tutulan düz string kopyadır ve
/// <c>AuthorizedBranches</c> setter'ı tarafından senkron tutulmalıdır (#119).
/// </summary>
public class BusinessBranchStateTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

    private static BusinessEntity NewBusiness() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Mezitli Elektrik Sanayi",
        Address = "Davultepe Mah., Mezitli, Mersin"
    };

    [Fact]
    public void Yeni_isletmenin_aktif_alan_listesi_bostur()
    {
        // Arrange & Act
        var business = NewBusiness();

        // Assert
        business.AuthorizedBranches.ShouldBeEmpty();
        business.ActiveBranchCodes.ShouldBeEmpty();
    }

    [Fact]
    public void AuthorizedBranches_atandiginda_ActiveBranchCodes_senkron_olur()
    {
        // Arrange
        var business = NewBusiness();

        // Act
        business.AuthorizedBranches = BranchAuthorizationPolicy.Apply(
            business.AuthorizedBranches,
            [new BranchAuthorizationRequest("EET"), new BranchAuthorizationRequest("MTT")],
            "Müdür",
            Now);

        // Assert
        business.ActiveBranchCodes.ShouldBe(["EET", "MTT"]);
    }

    [Fact]
    public void Yetki_iptal_edilince_ActiveBranchCodes_daralir_kayit_korunur()
    {
        // Arrange
        var business = NewBusiness();
        business.AuthorizedBranches = BranchAuthorizationPolicy.Apply(
            business.AuthorizedBranches,
            [new BranchAuthorizationRequest("EET"), new BranchAuthorizationRequest("MTT")],
            "Müdür",
            Now);

        // Act — idare yalnız EET'yi bırakıyor
        business.AuthorizedBranches = BranchAuthorizationPolicy.Apply(
            business.AuthorizedBranches, [new BranchAuthorizationRequest("EET")], "Müdür", Now.AddDays(1));

        // Assert
        business.ActiveBranchCodes.ShouldBe(["EET"]);
        business.AuthorizedBranches.Count.ShouldBe(2);
        business.AuthorizedBranches.Single(a => a.BranchCode == "MTT").RevokedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AuthorizedBranches_null_atanirsa_bos_listeye_dusulur()
    {
        // Arrange
        var business = NewBusiness();

        // Act
        business.AuthorizedBranches = null!;

        // Assert
        business.AuthorizedBranches.ShouldBeEmpty();
        business.ActiveBranchCodes.ShouldBeEmpty();
    }
}
