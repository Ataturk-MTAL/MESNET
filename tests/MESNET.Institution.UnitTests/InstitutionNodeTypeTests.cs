using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Enums;
using Shouldly;
using Xunit;
// "Institution" hem ad alanı hem tip adı olduğu için doğrudan kullanılamaz (CS0118).
// Depoda aynı kısayol InstitutionTenantDirectory içinde de var.
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Düğüm tipinin <b>çözümlenmesi</b> geriye dönük uyumluluğun tamamıdır.
///
/// <para>Mevcut kurum kayıtları bu alan olmadan saklandı ve hepsi okuldur. <c>Resolve(null)</c>
/// <c>School</c> döndürmezse, geçiş ucu koşturulana kadar okul listesi <b>boş</b> gelir — hata
/// değil, sessiz boşluk.</para>
///
/// <para><b>Neden entity'de SmartEnum saklanmıyor:</b> Marten LINQ'te <c>i.NodeType.Name</c>
/// SQL'e <c>data->'nodeType'->>'Name'</c> çevrilir; SmartEnum ise JSON'a düz string yazılır,
/// nesne değil. Sonuç HER ZAMAN NULL'dur ve sorgu hiçbir şey bulmaz. Bu yüzden stok alan tek
/// ve düzdür (<c>NodeTypeName</c>); SmartEnum ondan hesaplanır ve serialize EDİLMEZ.</para>
/// </summary>
public sealed class InstitutionNodeTypeTests
{
    [Fact]
    public void Uc_dugum_tipi_vardir()
    {
        InstitutionNodeType.List.Select(t => t.Name).ShouldBe(
            new[] { "Province", "District", "School" }, ignoreOrder: true);
    }

    [Fact]
    public void Her_tipin_turkce_etiketi_var()
    {
        foreach (var type in InstitutionNodeType.List)
            type.Slug.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("Province")]
    [InlineData("province")]
    [InlineData("PROVINCE")]
    public void Bilinen_ad_buyuk_kucuk_harfe_duyarsiz_cozulur(string name)
    {
        InstitutionNodeType.Resolve(name).ShouldBe(InstitutionNodeType.Province);
    }

    /// <summary>
    /// Geçiş koşturulmamış kayıt. Bu davranış olmadan okul listesi boş gelir.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_deger_okul_sayilir(string? name)
    {
        InstitutionNodeType.Resolve(name).ShouldBe(InstitutionNodeType.School);
    }

    /// <summary>
    /// Tanınmayan değer de en DAR okumaya düşer. Province sayılsaydı, bozuk tek bir satır
    /// kendine bir alt ağaç uydururdu.
    /// </summary>
    [Fact]
    public void Taninmayan_deger_okul_sayilir()
    {
        InstitutionNodeType.Resolve("Bakanlik").ShouldBe(InstitutionNodeType.School);
    }

    [Fact]
    public void Entity_dugum_tipini_stok_alandan_hesaplar()
    {
        var entity = new InstitutionRecord { FullName = "Test" };

        entity.NodeType.ShouldBe(InstitutionNodeType.School);

        entity.NodeTypeName = InstitutionNodeType.Province.Name;
        entity.NodeType.ShouldBe(InstitutionNodeType.Province);
    }

    [Fact]
    public void Eski_kayit_dto_ya_okul_olarak_cikar()
    {
        var entity = new InstitutionRecord
        {
            Id = Guid.NewGuid(),
            InstitutionCode = 967523,
            FullName = "Atatürk MTAL"
        };

        var dto = entity.ToDto();

        dto.NodeType.ShouldBe("School");
        dto.NodeTypeSlug.ShouldBe(InstitutionNodeType.School.Slug);
        dto.ParentId.ShouldBeNull();
        dto.ParentName.ShouldBeNull();
    }

    [Fact]
    public void Ust_dugum_adi_disaridan_verilir()
    {
        var entity = new InstitutionRecord
        {
            Id = Guid.NewGuid(),
            InstitutionCode = 967523,
            FullName = "Atatürk MTAL",
            ParentId = Guid.NewGuid(),
            NodeTypeName = InstitutionNodeType.School.Name
        };

        entity.ToDto(parentName: "Yenimahalle İlçe Millî Eğitim Müdürlüğü")
            .ParentName.ShouldBe("Yenimahalle İlçe Millî Eğitim Müdürlüğü");
    }
}
