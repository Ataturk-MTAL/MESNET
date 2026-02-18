using FluentValidation;
using MESNET.Enrollment.Application.Commands;

namespace MESNET.Enrollment.Application.Validators;

public class ApplyForInternshipValidator : AbstractValidator<ApplyForInternship>
{
    public ApplyForInternshipValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");
        RuleFor(x => x.BranchCode).NotEmpty().WithMessage("Alan kodu belirtilmelidir.");
    }
}
