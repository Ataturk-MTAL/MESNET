using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Validators;
using MESNET.Institution.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Yeni kurum açarken ağaçtaki yerin doğrulanması.
///
/// <para><b>Neden validator seviyesinde:</b> tanınmayan bir <c>nodeType</c> handler'a
/// ulaşırsa <c>InstitutionNodeType.Resolve</c> onu sessizce <c>School</c> yapar — kullanıcı
/// il müdürlüğü açtığını sanırken bir okul doğar ve bunu hiçbir hata bildirmez. Çözümleyicinin
/// hoşgörüsü OKUMA tarafı içindir; yazma sınırında küme kapalıdır.</para>
/// </summary>
public sealed class CreateInstitutionNodeValidationTests
{
    private static readonly CreateInstitutionValidator Validator = new();

    private static CreateInstitution Komut(
        string? nodeType = null, Guid? parentId = null, int code = 967523) =>
        new(code, "Test Kurumu", null, null, null, null, null,
            ProvinceCode: "06", DistrictName: "Yenimahalle",
            Id: null, NodeType: nodeType, ParentId: parentId);

    [Theory]
    [InlineData(null)]
    [InlineData("School")]
    [InlineData("District")]
    [InlineData("Province")]
    public void Bilinen_dugum_tipleri_kabul_edilir(string? nodeType)
    {
        Validator.Validate(Komut(nodeType, parentId: nodeType == "Province" ? null : Guid.NewGuid()))
            .Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateInstitution.NodeType));
    }

    /// <summary>
    /// Tanınmayan tip REDDEDİLİR. Resolve onu sessizce School yapardı ve kullanıcı il
    /// müdürlüğü açtığını sanırken bir okul doğardı.
    /// </summary>
    [Fact]
    public void Taninmayan_dugum_tipi_reddedilir()
    {
        Validator.Validate(Komut("Bakanlik"))
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateInstitution.NodeType));
    }

    /// <summary>
    /// İl müdürlüğü kökündür; üstü olamaz. İzin verilseydi ağaç modellenen üç seviyeyi aşar
    /// ve "il yetkilisinin üstündeki il yetkilisi" gibi anlamsız bir kapsam doğardı.
    /// </summary>
    [Fact]
    public void Il_dugumunun_ustu_olamaz()
    {
        Validator.Validate(Komut(InstitutionNodeType.Province.Name, parentId: Guid.NewGuid()))
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateInstitution.ParentId));
    }

    /// <summary>
    /// Üst düğümlerin MEB kurum kodu bu geçişin elinde yok; sıfır "girilmedi" demektir.
    /// Okul için kural değişmez — kod zorunludur.
    /// </summary>
    [Fact]
    public void Ust_dugum_kurum_kodusuz_acilabilir_okul_acilamaz()
    {
        Validator.Validate(Komut(InstitutionNodeType.Province.Name, code: 0))
            .Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateInstitution.InstitutionCode));

        Validator.Validate(Komut(InstitutionNodeType.School.Name, parentId: Guid.NewGuid(), code: 0))
            .Errors.ShouldContain(e => e.PropertyName == nameof(CreateInstitution.InstitutionCode));
    }
}
