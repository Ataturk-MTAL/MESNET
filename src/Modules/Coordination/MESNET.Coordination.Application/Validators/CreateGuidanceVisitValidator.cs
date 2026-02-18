using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class CreateGuidanceVisitValidator : AbstractValidator<CreateGuidanceVisit>
{
    public CreateGuidanceVisitValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty().WithMessage("Öğretmen belirtilmelidir.");
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.VisitDate).NotEmpty().WithMessage("Ziyaret tarihi belirtilmelidir.");
        RuleFor(x => x.StudentNotes).NotEmpty().WithMessage("En az bir öğrenci notu belirtilmelidir.");
    }
}
