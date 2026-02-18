using FluentValidation;
using MESNET.Contract.Application.Commands;

namespace MESNET.Contract.Application.Validators;

public class SuspendContractValidator : AbstractValidator<SuspendContract>
{
    public SuspendContractValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty().WithMessage("Sözleşme belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Askıya alma gerekçesi belirtilmelidir.");
    }
}
