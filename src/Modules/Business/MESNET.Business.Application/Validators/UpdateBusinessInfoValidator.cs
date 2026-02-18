using FluentValidation;
using MESNET.Business.Application.Commands;

namespace MESNET.Business.Application.Validators;

public class UpdateBusinessInfoValidator : AbstractValidator<UpdateBusinessInfo>
{
    public UpdateBusinessInfoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("İşletme adı belirtilmelidir.")
            .MaximumLength(200).WithMessage("İşletme adı en fazla 200 karakter olmalıdır.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Adres belirtilmelidir.");
        RuleFor(x => x.PersonnelCount).GreaterThan(0).WithMessage("Personel sayısı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}
