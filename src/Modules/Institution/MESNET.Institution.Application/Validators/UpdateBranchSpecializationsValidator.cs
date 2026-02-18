using FluentValidation;
using MESNET.Institution.Application.Commands;

namespace MESNET.Institution.Application.Validators;

public class UpdateBranchSpecializationsValidator : AbstractValidator<UpdateBranchSpecializations>
{
    public UpdateBranchSpecializationsValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.FieldCode).NotEmpty().WithMessage("Alan kodu belirtilmelidir.");
        RuleFor(x => x.ActiveSpecializations).NotEmpty().WithMessage("En az bir uzmanlık alanı belirtilmelidir.");
    }
}
