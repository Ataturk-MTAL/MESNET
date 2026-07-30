using FluentValidation;
using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Commands;

namespace MESNET.Institution.Application.Validators;

public class CreateInstitutionValidator : AbstractValidator<CreateInstitution>
{
    public CreateInstitutionValidator()
    {
        RuleFor(x => x.InstitutionCode).GreaterThan(0).WithMessage("Kurum kodu sıfırdan büyük olmalıdır.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Kurum adı belirtilmelidir.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");

        // İl kodu kayıt anında ZORUNLU (#147). Sonradan doldurulabilir bırakılırsa ikinci il
        // eklendiğinde ayrım yapılamayan kayıtlar birikir ve elle backfill gerekir.
        RuleFor(x => x.ProvinceCode).NotEmpty().WithMessage("İl belirtilmelidir.");
        RuleFor(x => x.ProvinceCode)
            .Must(TurkishProvinces.IsValidCode)
            .When(x => !string.IsNullOrWhiteSpace(x.ProvinceCode))
            .WithMessage("Geçerli bir MEB il kodu giriniz (01–81).");

        // İlçe ile il birlikte doğrulanır: ilçe adı TEK BAŞINA anlamlı değildir, hangi ilin
        // ilçesi olduğu bilinmeden doğrulanamaz. Aynı ad birden çok ilde geçebilir.
        RuleFor(x => x.DistrictName)
            .Must((command, district) => TurkishDistricts.IsValid(command.ProvinceCode, district))
            .When(x => !string.IsNullOrWhiteSpace(x.DistrictName))
            .WithMessage(x => DistrictMessage(x.ProvinceCode));
    }

    /// <summary>
    /// İlçe listesi bulunmayan il ile ilçe listesinde olmayan ad ayrı hatalardır: ilki eksik
    /// referans verisi (çözümü listeyi doldurmak), ikincisi hatalı giriş. Aynı mesajı vermek
    /// kullanıcıyı olmayan bir yazım hatasını aramaya iterdi.
    /// </summary>
    internal static string DistrictMessage(string? provinceCode) =>
        TurkishDistricts.IsKnown(provinceCode)
            ? "Seçilen ile ait geçerli bir ilçe seçiniz."
            : "Bu il için ilçe listesi tanımlı değil, ilçe girilemez.";
}
