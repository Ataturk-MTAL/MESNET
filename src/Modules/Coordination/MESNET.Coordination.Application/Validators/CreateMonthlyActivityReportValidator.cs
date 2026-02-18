using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class CreateMonthlyActivityReportValidator : AbstractValidator<CreateMonthlyActivityReport>
{
    public CreateMonthlyActivityReportValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.TeacherId).NotEmpty().WithMessage("Öğretmen belirtilmelidir.");
        RuleFor(x => x.Year).GreaterThan(0).WithMessage("Geçerli bir yıl belirtilmelidir.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Ay 1-12 arasında olmalıdır.");
        RuleFor(x => x.Activities).NotEmpty().WithMessage("En az bir günlük faaliyet belirtilmelidir.");
    }
}
