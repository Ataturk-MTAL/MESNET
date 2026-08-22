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

    private static CreateInstitution Command(string? provinceCode = "33", string? districtName = null) =>
        new(967523, "Atatürk Mesleki ve Teknik Anadolu Lisesi",
            "Toroslar, Mersin", null, null, null, (Location?)null, provinceCode, districtName);

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
    public void accepts_a_null_district()
    {
        // İlçe zorunlu değil — kayıt il ile açılabilir, ilçe sonradan girilebilir.
        Validator.Validate(Command(districtName: null)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void accepts_a_district_of_the_selected_province()
    {
        Validator.Validate(Command(districtName: "Toroslar")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("toroslar")]   // küçük harf — aynı ilçenin ikinci yazımı olur
    [InlineData("TOROSLAR")]
    [InlineData("Toroslar ")]  // sonda boşluk
    [InlineData("Torroslar")]  // yazım hatası
    public void rejects_a_district_that_is_not_written_exactly_as_listed(string districtName)
    {
        Validator.Validate(Command(districtName: districtName)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void rejects_a_district_belonging_to_a_different_province()
    {
        // Çankaya Ankara'nın ilçesi; Mersin seçiliyken kabul edilemez.
        Validator.Validate(Command(provinceCode: "33", districtName: "Çankaya")).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void accepts_a_district_of_any_of_the_81_provinces()
    {
        // Liste 81 ilin tamamını kapsar; Mersin dışındaki iller de kayıt açabilir.
        Validator.Validate(Command(provinceCode: "06", districtName: "Çankaya")).IsValid.ShouldBeTrue();
        Validator.Validate(Command(provinceCode: "34", districtName: "Kadıköy")).IsValid.ShouldBeTrue();
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

    private static UpdateInstitution Command(
        string? provinceCode = null, string? districtName = null, int? institutionCode = null) =>
        new(InstitutionId, "Atatürk Mesleki ve Teknik Anadolu Lisesi",
            null, null, null, null, (Location?)null, provinceCode, districtName, institutionCode);

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
    public void rejects_a_district_that_does_not_belong_to_the_province_sent_with_it()
    {
        Validator.Validate(Command(provinceCode: "33", districtName: "Çankaya")).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void accepts_a_district_sent_alone_and_leaves_the_check_to_the_handler()
    {
        // İl gönderilmediğinde hangi ile ait olduğu istekte YOKTUR; doğrulayıcı bunu bilemez.
        // Kombinasyon kontrolü UpdateInstitutionHandler'da, mevcut kurumun ili okunarak yapılır.
        Validator.Validate(Command(districtName: "Çankaya")).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void accepts_an_omitted_institution_code()
    {
        // Kurum kodu kayıtta girilir, sonradan düzeltilebilir — güncellemede zorunlu değil.
        Validator.Validate(Command()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void rejects_a_non_positive_institution_code(int institutionCode)
    {
        Validator.Validate(Command(institutionCode: institutionCode)).IsValid.ShouldBeFalse();
    }
}
