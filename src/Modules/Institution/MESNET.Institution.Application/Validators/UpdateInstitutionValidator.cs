using FluentValidation;
using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Commands;

namespace MESNET.Institution.Application.Validators;

public class UpdateInstitutionValidator : AbstractValidator<UpdateInstitution>
{
    public UpdateInstitutionValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Kurum adı belirtilmelidir.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");

        // Güncellemede null = "değiştirme" olduğu için NotEmpty YOK; ama gelen değer varsa
        // geçerli olmak zorunda — aksi hâlde serbest metin il adı bu uçtan sızar.
        RuleFor(x => x.ProvinceCode)
            .Must(TurkishProvinces.IsValidCode)
            .When(x => x.ProvinceCode is not null)
            .WithMessage("Geçerli bir MEB il kodu giriniz (01–81).");

        RuleFor(x => x.DistrictCode)
            .Must(CreateInstitutionValidator.BeADistrictCode)
            .When(x => x.DistrictCode is not null)
            .WithMessage("İlçe kodu yalnız rakam içerebilir.");
    }
}
