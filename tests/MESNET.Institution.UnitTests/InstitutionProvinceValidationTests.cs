using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Validators;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Kurum kaydında il kodu ZORUNLU (#147): sonradan doldurulabilir bırakılırsa ikinci il
/// eklendiğinde ayrım yapılamayan kayıtlar birikir ve elle backfill gerekir.
/// </summary>
public class CreateInstitutionProvinceValidationTests
{
    private static readonly CreateInstitutionValidator Validator = new();

    private static CreateInstitution Command(string? provinceCode = "33", string? districtCode = null) =>
        new(967523, "Atatürk Mesleki ve Teknik Anadolu Lisesi",
            "Toroslar, Mersin", null, null, null, (Location?)null, provinceCode, districtCode);

    [Fact]
    public void accepts_a_valid_province_code()
    {
        Validator.Validate(Command()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void rejects_a_missing_province_code(string? provinceCode)
    {
        var result = Validator.Validate(Command(provinceCode));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateInstitution.ProvinceCode));
    }

    [Theory]
    [InlineData("82")]
    [InlineData("00")]
    [InlineData("1")]
    [InlineData("Mersin")]
    public void rejects_an_unknown_province_code(string provinceCode)
    {
        Validator.Validate(Command(provinceCode)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void accepts_a_null_district_code()
    {
        // İlçe kapsamı henüz karara bağlanmadı (#147) — alan var, zorunlu değil.
        Validator.Validate(Command(districtCode: null)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void accepts_a_numeric_district_code()
    {
        Validator.Validate(Command(districtCode: "123456")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Toroslar")]
    [InlineData("33-01")]
    [InlineData("12a")]
    public void rejects_a_non_numeric_district_code(string districtCode)
    {
        Validator.Validate(Command(districtCode: districtCode)).IsValid.ShouldBeFalse();
    }
}

/// <summary>
/// Güncellemede <c>null</c> = "değiştirme" olduğu için il kodu NotEmpty değildir; ama gelen
/// değer geçersizse reddedilmek zorundadır — aksi hâlde serbest metin il adı bu uçtan sızar.
/// </summary>
public class UpdateInstitutionProvinceValidationTests
{
    private static readonly UpdateInstitutionValidator Validator = new();
    private static readonly Guid InstitutionId = Guid.NewGuid();

    private static UpdateInstitution Command(string? provinceCode = null, string? districtCode = null) =>
        new(InstitutionId, "Atatürk Mesleki ve Teknik Anadolu Lisesi",
            null, null, null, null, (Location?)null, provinceCode, districtCode);

    [Fact]
    public void accepts_an_omitted_province_code()
    {
        Validator.Validate(Command()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void accepts_a_valid_province_code()
    {
        Validator.Validate(Command(provinceCode: "06")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]        // boş dize "temizle" demek değildir; il temizlenemez
    [InlineData("82")]
    [InlineData("Ankara")]
    public void rejects_an_invalid_province_code(string provinceCode)
    {
        Validator.Validate(Command(provinceCode)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void rejects_a_non_numeric_district_code()
    {
        Validator.Validate(Command(districtCode: "Çankaya")).IsValid.ShouldBeFalse();
    }
}
