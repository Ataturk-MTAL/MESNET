using FluentValidation;
using MESNET.Contract.Application.Commands;

namespace MESNET.Contract.Application.Validators;

public class SignContractValidator : AbstractValidator<SignContract>
{
    public SignContractValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty().WithMessage("Sözleşme belirtilmelidir.");
        RuleFor(x => x.Party).NotEmpty().WithMessage("İmzalayan taraf belirtilmelidir.");
        RuleFor(x => x.SignedBy).NotEmpty().WithMessage("İmzalayan kişi belirtilmelidir.");
    }
}
