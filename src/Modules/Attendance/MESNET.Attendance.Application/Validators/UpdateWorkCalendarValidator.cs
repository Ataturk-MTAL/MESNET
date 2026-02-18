using FluentValidation;
using MESNET.Attendance.Application.Commands;

namespace MESNET.Attendance.Application.Validators;

public class UpdateWorkCalendarValidator : AbstractValidator<UpdateWorkCalendar>
{
    public UpdateWorkCalendarValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.Year).GreaterThan(0).WithMessage("Geçerli bir yıl belirtilmelidir.");
        RuleFor(x => x.RestrictedDays).NotEmpty().WithMessage("En az bir kısıtlı gün belirtilmelidir.");
        RuleFor(x => x.UpdatedBy).NotEmpty().WithMessage("Güncelleyen kişi belirtilmelidir.");
    }
}
