using FluentValidation;
using MESNET.Enrollment.Application.Commands;

namespace MESNET.Enrollment.Application.Validators;

public class RequestStudentValidator : AbstractValidator<RequestStudent>
{
    public RequestStudentValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.BranchCode).NotEmpty().WithMessage("Alan kodu belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
    }
}
