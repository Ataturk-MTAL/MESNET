using FluentValidation;
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
    }
}
