using FluentValidation;
using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Commands;

namespace MESNET.Institution.Application.Validators;

public class CreateInstitutionValidator : AbstractValidator<CreateInstitution>
{
    public CreateInstitutionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Kurum (tenant) belirtilmelidir.");
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

        RuleFor(x => x.DistrictCode)
            .Must(BeADistrictCode)
            .When(x => !string.IsNullOrWhiteSpace(x.DistrictCode))
            .WithMessage("İlçe kodu yalnız rakam içerebilir.");
    }

    /// <summary>
    /// MEB ilçe kodu yalnız rakamdan oluşur; uzunluğu ile ilgili doğrulanmış bir kaynak
    /// olmadığı için hane sayısı kısıtlanmaz — yanlış bir üst sınır geçerli kaydı reddederdi.
    /// </summary>
    internal static bool BeADistrictCode(string? code) =>
        code is not null && code.All(char.IsAsciiDigit);
}
