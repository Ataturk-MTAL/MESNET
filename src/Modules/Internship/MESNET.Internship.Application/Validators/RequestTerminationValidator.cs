using FluentValidation;
using MESNET.Internship.Application.Commands;

namespace MESNET.Internship.Application.Validators;

public class RequestTerminationValidator : AbstractValidator<RequestTermination>
{
    public RequestTerminationValidator()
    {
        RuleFor(x => x.InternshipId).NotEmpty().WithMessage("Staj belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Fesih gerekçesi belirtilmelidir.");
        RuleFor(x => x.ReasonType).NotEmpty().WithMessage("Fesih nedeni türü belirtilmelidir.");
        RuleFor(x => x.RequestedBy).NotEmpty().WithMessage("Talep eden kişi belirtilmelidir.");
    }
}
