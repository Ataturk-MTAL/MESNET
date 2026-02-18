using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class CreateBusinessEvaluationValidator : AbstractValidator<CreateBusinessEvaluation>
{
    public CreateBusinessEvaluationValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.EvaluatorId).NotEmpty().WithMessage("Değerlendirici belirtilmelidir.");
        RuleFor(x => x.EvaluationDate).NotEmpty().WithMessage("Değerlendirme tarihi belirtilmelidir.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("En az bir değerlendirme maddesi belirtilmelidir.");
        RuleFor(x => x.Result).NotEmpty().WithMessage("Değerlendirme sonucu belirtilmelidir.");
    }
}
