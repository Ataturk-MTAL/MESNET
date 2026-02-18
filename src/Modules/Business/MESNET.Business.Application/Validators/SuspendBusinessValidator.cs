using FluentValidation;
using MESNET.Business.Application.Commands;

namespace MESNET.Business.Application.Validators;

public class SuspendBusinessValidator : AbstractValidator<SuspendBusiness>
{
    public SuspendBusinessValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.SuspendedBy).NotEmpty().WithMessage("Askıya alan kişi belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Askıya alma gerekçesi belirtilmelidir.");
    }
}
