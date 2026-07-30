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

        // Yalnız il ve ilçe BİRLİKTE gönderildiğinde çapraz doğrulanabilir. İlçe tek başına
        // gelirse hangi ile ait olduğu istekte yoktur — o kontrol handler'da, mevcut kurumun
        // ili okunarak yapılır (UpdateInstitutionHandler).
        RuleFor(x => x.DistrictName)
            .Must((command, district) => TurkishDistricts.IsValid(command.ProvinceCode, district))
            .When(x => x.DistrictName is not null && x.ProvinceCode is not null)
            .WithMessage(x => CreateInstitutionValidator.DistrictMessage(x.ProvinceCode));

        RuleFor(x => x.InstitutionCode!.Value)
            .GreaterThan(0).WithMessage("Kurum kodu sıfırdan büyük olmalıdır.")
            .When(x => x.InstitutionCode is not null);
    }
}
