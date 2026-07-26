using MESNET.Business.Core.Policies;
using MESNET.Business.Core.ValueObjects;
using Shouldly;
using Xunit;

namespace MESNET.Business.UnitTests;

/// <summary>Alan yetkisi kuralları (#119) — aktif/iptal ayrımı, yerine koyma ve geçiş dolgusu.</summary>
public class BranchAuthorizationPolicyTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

    private static BranchAuthorization Active(string code, Guid? documentId = null) => new()
    {
        BranchCode = code,
        BasedOnDocumentId = documentId,
        AuthorizedAt = Now.AddDays(-30),
        AuthorizedBy = "Müdür Yardımcısı"
    };

    private static BranchAuthorization Revoked(string code) => Active(code) with { RevokedAt = Now.AddDays(-1) };

    [Fact]
    public void ActiveCodes_yetki_yoksa_bos_liste_doner_kapali_demektir()
    {
        // Arrange
        var authorizations = new List<BranchAuthorization>();

        // Act
        var codes = BranchAuthorizationPolicy.ActiveCodes(authorizations);

        // Assert
        codes.ShouldBeEmpty();
    }

    [Fact]
    public void ActiveCodes_iptal_edilmis_yetkileri_disarida_birakir()
    {
        // Arrange
        var authorizations = new List<BranchAuthorization> { Active("EET"), Revoked("BT") };

        // Act
        var codes = BranchAuthorizationPolicy.ActiveCodes(authorizations);

        // Assert
        codes.ShouldBe(["EET"]);
    }

    [Fact]
    public void IsAuthorizedFor_aktif_alan_icin_true_iptal_edilmis_alan_icin_false_doner()
    {
        // Arrange
        var authorizations = new List<BranchAuthorization> { Active("EET"), Revoked("BT") };

        // Act & Assert
        BranchAuthorizationPolicy.IsAuthorizedFor(authorizations, "EET").ShouldBeTrue();
        BranchAuthorizationPolicy.IsAuthorizedFor(authorizations, "BT").ShouldBeFalse();
        BranchAuthorizationPolicy.IsAuthorizedFor(authorizations, "MTT").ShouldBeFalse();
    }

    [Fact]
    public void IsAuthorizedFor_alan_kodu_bos_ise_false_doner()
    {
        // Arrange
        var authorizations = new List<BranchAuthorization> { Active("EET") };

        // Act & Assert
        BranchAuthorizationPolicy.IsAuthorizedFor(authorizations, null).ShouldBeFalse();
        BranchAuthorizationPolicy.IsAuthorizedFor(authorizations, "   ").ShouldBeFalse();
    }

    [Fact]
    public void Apply_listede_olmayan_aktif_yetkiyi_iptal_eder_kaydi_silmez()
    {
        // Arrange
        var current = new List<BranchAuthorization> { Active("EET"), Active("BT") };

        // Act — idare yalnız EET'yi işaretledi
        var result = BranchAuthorizationPolicy.Apply(
            current, [new BranchAuthorizationRequest("EET")], "Müdür", Now);

        // Assert
        result.Count.ShouldBe(2);
        result.Single(a => a.BranchCode == "BT").RevokedAt.ShouldBe(Now);
        result.Single(a => a.BranchCode == "EET").IsActive.ShouldBeTrue();
        BranchAuthorizationPolicy.ActiveCodes(result).ShouldBe(["EET"]);
    }

    [Fact]
    public void Apply_yeni_alani_dayanak_belgesiyle_ekler()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = BranchAuthorizationPolicy.Apply(
            [], [new BranchAuthorizationRequest("MTT", documentId)], "Müdür", Now);

        // Assert
        var added = result.ShouldHaveSingleItem();
        added.BranchCode.ShouldBe("MTT");
        added.BasedOnDocumentId.ShouldBe(documentId);
        added.AuthorizedBy.ShouldBe("Müdür");
        added.AuthorizedAt.ShouldBe(Now);
        added.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Apply_bos_liste_gonderilirse_tum_yetkileri_iptal_eder()
    {
        // Arrange
        var current = new List<BranchAuthorization> { Active("EET"), Active("BT") };

        // Act
        var result = BranchAuthorizationPolicy.Apply(current, [], "Müdür", Now);

        // Assert
        BranchAuthorizationPolicy.ActiveCodes(result).ShouldBeEmpty();
        result.ShouldAllBe(a => a.RevokedAt == Now);
    }

    [Fact]
    public void Apply_dayanak_belge_degismediyse_mevcut_kaydi_tazelemez()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var existing = Active("EET", documentId);

        // Act
        var result = BranchAuthorizationPolicy.Apply(
            [existing], [new BranchAuthorizationRequest("EET", documentId)], "Başka Müdür", Now);

        // Assert
        var kept = result.ShouldHaveSingleItem();
        kept.AuthorizedAt.ShouldBe(existing.AuthorizedAt);
        kept.AuthorizedBy.ShouldBe(existing.AuthorizedBy);
    }

    [Fact]
    public void Apply_dayanak_belge_degistiyse_kaydi_tazeler()
    {
        // Arrange
        var existing = Active("EET", Guid.NewGuid());
        var newDocumentId = Guid.NewGuid();

        // Act
        var result = BranchAuthorizationPolicy.Apply(
            [existing], [new BranchAuthorizationRequest("EET", newDocumentId)], "Yeni Müdür", Now);

        // Assert
        var refreshed = result.ShouldHaveSingleItem();
        refreshed.BasedOnDocumentId.ShouldBe(newDocumentId);
        refreshed.AuthorizedBy.ShouldBe("Yeni Müdür");
        refreshed.AuthorizedAt.ShouldBe(Now);
        refreshed.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Apply_iptal_edilmis_alan_yeniden_isaretlenirse_yeniden_aktif_yetki_uretir()
    {
        // Arrange
        var current = new List<BranchAuthorization> { Revoked("BT") };

        // Act
        var result = BranchAuthorizationPolicy.Apply(
            current, [new BranchAuthorizationRequest("BT")], "Müdür", Now);

        // Assert — eski iptal kaydı denetim izi olarak korunur, yanına yeni aktif kayıt eklenir
        result.Count.ShouldBe(2);
        BranchAuthorizationPolicy.ActiveCodes(result).ShouldBe(["BT"]);
    }

    [Fact]
    public void Merge_geçis_dolgusu_yalniz_eksik_alanlari_ekler_hicbirini_iptal_etmez()
    {
        // Arrange
        var current = new List<BranchAuthorization> { Active("EET") };

        // Act
        var result = BranchAuthorizationPolicy.Merge(current, ["EET", "MTT"], "Sistem", Now);

        // Assert
        BranchAuthorizationPolicy.ActiveCodes(result).ShouldBe(["EET", "MTT"]);
        result.Single(a => a.BranchCode == "EET").AuthorizedBy.ShouldBe("Müdür Yardımcısı");
        result.Single(a => a.BranchCode == "MTT").AuthorizedBy.ShouldBe("Sistem");
    }

    [Fact]
    public void Merge_iptal_edilmis_alan_dolguda_yeniden_yetkilendirilir()
    {
        // Arrange — yetki iptal edilmiş ama işletmede o alandan fiilî yerleştirme var
        var current = new List<BranchAuthorization> { Revoked("BT") };

        // Act
        var result = BranchAuthorizationPolicy.Merge(current, ["BT"], "Sistem", Now);

        // Assert
        BranchAuthorizationPolicy.ActiveCodes(result).ShouldBe(["BT"]);
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Merge_aynı_alan_tekrar_gelirse_kopya_yetki_uretmez()
    {
        // Arrange
        var current = new List<BranchAuthorization> { Active("EET") };

        // Act
        var result = BranchAuthorizationPolicy.Merge(current, ["EET", "eet", " EET "], "Sistem", Now);

        // Assert
        result.ShouldHaveSingleItem();
    }
}
