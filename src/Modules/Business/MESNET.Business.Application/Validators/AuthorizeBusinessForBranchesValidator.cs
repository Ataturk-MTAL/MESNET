using FluentValidation;
using MESNET.Business.Application.Commands;

namespace MESNET.Business.Application.Validators;

public class AuthorizeBusinessForBranchesValidator : AbstractValidator<AuthorizeBusinessForBranches>
{
    public AuthorizeBusinessForBranchesValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");

        // Boş liste geçerlidir: "hiçbir alandan öğrenci alamaz" anlamına gelir (#119).
        RuleForEach(x => x.Branches)
            .Must(b => !string.IsNullOrWhiteSpace(b.BranchCode))
            .When(x => x.Branches is not null)
            .WithMessage("Alan kodu boş olamaz.");
    }
}
