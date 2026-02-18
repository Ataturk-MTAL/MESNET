using FluentValidation;
using MESNET.Payment.Application.Commands;

namespace MESNET.Payment.Application.Validators;

public class UpdateMinimumWageValidator : AbstractValidator<UpdateMinimumWage>
{
    public UpdateMinimumWageValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.NewMinimumWage).GreaterThan(0).WithMessage("Asgari ücret sıfırdan büyük olmalıdır.");
        RuleFor(x => x.UpdatedBy).NotEmpty().WithMessage("Güncelleyen kişi belirtilmelidir.");
    }
}
