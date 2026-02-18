using FluentValidation;
using MESNET.Payment.Application.Commands;

namespace MESNET.Payment.Application.Validators;

public class RejectReceiptValidator : AbstractValidator<RejectReceipt>
{
    public RejectReceiptValidator()
    {
        RuleFor(x => x.SalaryPeriodId).NotEmpty().WithMessage("Maaş dönemi belirtilmelidir.");
        RuleFor(x => x.RejectedBy).NotEmpty().WithMessage("Reddeden kişi belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Red gerekçesi belirtilmelidir.");
    }
}
