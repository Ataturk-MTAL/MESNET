using FluentValidation;
using MESNET.Internship.Application.Commands;

namespace MESNET.Internship.Application.Validators;

public class OverrideTerminationApprovalValidator : AbstractValidator<OverrideTerminationApproval>
{
    public OverrideTerminationApprovalValidator()
    {
        RuleFor(x => x.InternshipId).NotEmpty().WithMessage("Staj belirtilmelidir.");
        RuleFor(x => x.OverriddenBy).NotEmpty().WithMessage("Onaylayan kişi belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Gerekçe belirtilmelidir.");
    }
}
