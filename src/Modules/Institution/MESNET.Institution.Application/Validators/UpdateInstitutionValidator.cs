using FluentValidation;
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
    }
}
