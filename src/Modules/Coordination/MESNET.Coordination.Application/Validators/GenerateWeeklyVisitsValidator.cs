using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class GenerateWeeklyVisitsValidator : AbstractValidator<GenerateWeeklyVisits>
{
    public GenerateWeeklyVisitsValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.AcademicPeriodId).NotEmpty().WithMessage("Eğitim dönemi belirtilmelidir.");
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100).WithMessage("Yıl 2020-2100 arasında olmalıdır.");
        RuleFor(x => x.WeekNumber).InclusiveBetween(1, 53).WithMessage("Hafta numarası 1-53 arasında olmalıdır.");
        RuleFor(x => x.Scope).Must(s => s is "Teacher" or "Branch" or "All")
            .WithMessage("Kapsam Teacher, Branch veya All olmalıdır.");
        RuleFor(x => x.TeacherId).NotEmpty()
            .When(x => x.Scope == "Teacher")
            .WithMessage("Öğretmen kapsamı seçildiğinde öğretmen belirtilmelidir.");
        RuleFor(x => x.BranchCode).NotEmpty()
            .When(x => x.Scope == "Branch")
            .WithMessage("Alan kapsamı seçildiğinde alan kodu belirtilmelidir.");
        RuleFor(x => x.GeneratedBy).NotEmpty().WithMessage("Oluşturan kullanıcı belirtilmelidir.");
    }
}
