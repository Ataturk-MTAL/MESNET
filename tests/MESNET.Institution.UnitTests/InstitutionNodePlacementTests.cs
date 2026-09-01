using MESNET.Common.Shared.Security;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// <see cref="InstitutionNodePlacement.Resolve"/> saf karar fonksiyonunun testleri.
///
/// <para><b>Neden burada:</b> handler'ın en riskli mantığı — üst yok/üst bulunamadı/üst
/// yolsuz/üst geçerli dalları — canlı bir Marten oturumu olmadan yalnız burada sınanabilir.
/// Depoda mock kütüphanesi yok; bu yüzden karar handler'dan saf bir fonksiyona çıkarıldı
/// (<c>InstitutionScopePolicy.Decide</c> ve <c>InstitutionHierarchyPlanner.Plan</c> ile aynı
/// idiom).</para>
/// </summary>
public sealed class InstitutionNodePlacementTests
{
    /// <summary>
    /// Üst verilmeden açılan bir İL kök olmalıdır — ağaçta başka hiçbir düğüm ondan üst
    /// olamaz. Kök yol biçimi <see cref="InstitutionPath.Root"/> ile aynı olmalı ki alt ağaç
    /// sorgusu (Path.StartsWith) daha ilk düğümden çalışsın.
    /// </summary>
    [Fact]
    public void Ust_verilmeden_il_acilirsa_kok_yol_alir()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = InstitutionNodePlacement.Resolve(
            InstitutionNodeType.Province, id, parentId: null, parentExists: false, parentPath: null);

        // Assert
        result.Outcome.ShouldBe(NodePlacementOutcome.Ok);
        result.Path.ShouldBe(InstitutionPath.Root(id));
    }

    /// <summary>
    /// Üst verilmeden açılan bir OKUL yolsuz doğar — geçiş ucu (rebuild-hierarchy) onu
    /// sonradan mevcut okul kaydı üzerinden dolduracaktır. Bu, bugünkü (geçişsiz) kayıtlarla
    /// aynı durumdur; hata sayılmaz.
    /// </summary>
    [Fact]
    public void Ust_verilmeden_okul_acilirsa_yolsuz_dogar()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = InstitutionNodePlacement.Resolve(
            InstitutionNodeType.School, id, parentId: null, parentExists: false, parentPath: null);

        // Assert
        result.Outcome.ShouldBe(NodePlacementOutcome.Ok);
        result.Path.ShouldBeNull();
    }

    /// <summary>
    /// Üst verilmeden açılan bir İLÇE de yolsuz doğar — yalnız İL kök alma ayrıcalığına
    /// sahiptir, ilçe değil. Aynı gerekçe okulla paylaşılır.
    /// </summary>
    [Fact]
    public void Ust_verilmeden_ilce_acilirsa_yolsuz_dogar()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = InstitutionNodePlacement.Resolve(
            InstitutionNodeType.District, id, parentId: null, parentExists: false, parentPath: null);

        // Assert
        result.Outcome.ShouldBe(NodePlacementOutcome.Ok);
        result.Path.ShouldBeNull();
    }

    /// <summary>
    /// Verilen üst düğüm veritabanında bulunamadıysa ağaç bağı kurulamaz; kayıt YARATILMAZ.
    /// Bulunamayan bir üste yol kurgulamak, var olmayan bir kayda referans veren ve hiçbir
    /// kapsamda görünmeyen bir çocuk bırakırdı.
    /// </summary>
    [Fact]
    public void Ust_verildi_ama_bulunamadiysa_ParentMissing_doner()
    {
        // Arrange
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        // Act
        var result = InstitutionNodePlacement.Resolve(
            InstitutionNodeType.School, id, parentId, parentExists: false, parentPath: null);

        // Assert
        result.Outcome.ShouldBe(NodePlacementOutcome.ParentMissing);
        result.Path.ShouldBeNull();
    }

    /// <summary>
    /// Üst bulundu ama yolu boş — geçiş ucu (rebuild-hierarchy) o kayıt için henüz koşmamış.
    /// Yolsuz bir üstün altına düğüm eklenirse çocuğun yolu da kurulamaz ve İKİSİ de hiçbir
    /// kapsamda görünmez — hata değil, sessiz boşluk. Bu yüzden reddedilir.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ust_bulundu_ama_yolu_bossa_ParentHasNoPath_doner(string? parentPath)
    {
        // Arrange
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        // Act
        var result = InstitutionNodePlacement.Resolve(
            InstitutionNodeType.School, id, parentId, parentExists: true, parentPath);

        // Assert
        result.Outcome.ShouldBe(NodePlacementOutcome.ParentHasNoPath);
        result.Path.ShouldBeNull();
    }

    /// <summary>
    /// Üst bulundu ve yolu doluysa çocuğun yolu üstün yolunun üzerine kurulur — alt ağaç
    /// sorgusu (Path.StartsWith(üstünYolu)) çocuğu da yakalasın diye üst önek olarak korunur
    /// ve kendi kimliği yeni bir segment olarak eklenir.
    /// </summary>
    [Fact]
    public void Ust_bulundu_ve_yolu_doluysa_yol_ustun_yolunun_uzerine_kurulur()
    {
        // Arrange
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var parentPath = InstitutionPath.Root(parentId);

        // Act
        var result = InstitutionNodePlacement.Resolve(
            InstitutionNodeType.District, id, parentId, parentExists: true, parentPath);

        // Assert
        result.Outcome.ShouldBe(NodePlacementOutcome.Ok);
        result.Path.ShouldNotBeNull();
        result.Path.ShouldStartWith(parentPath);
        result.Path.ShouldEndWith(InstitutionPath.Separator.ToString());
        result.Path.ShouldContain(id.ToString("D"));
    }
}
