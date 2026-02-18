using FluentValidation;
using MESNET.Contract.Application.Commands;

namespace MESNET.Contract.Application.Validators;

public class TerminateContractValidator : AbstractValidator<TerminateContract>
{
    public TerminateContractValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty().WithMessage("Sözleşme belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Fesih gerekçesi belirtilmelidir.");
        RuleFor(x => x.ReasonType).NotEmpty().WithMessage("Fesih nedeni türü belirtilmelidir.");
    }
}
