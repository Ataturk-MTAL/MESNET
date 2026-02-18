using FluentValidation;
using MESNET.Coordination.Application.Commands;

namespace MESNET.Coordination.Application.Validators;

public class UpsertTeacherScheduleValidator : AbstractValidator<UpsertTeacherSchedule>
{
    public UpsertTeacherScheduleValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty().WithMessage("Öğretmen belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.AcademicYear).GreaterThan(0).WithMessage("Geçerli bir öğretim yılı belirtilmelidir.");
        RuleFor(x => x.Semester).NotEmpty().WithMessage("Dönem belirtilmelidir.");
        RuleFor(x => x.WeeklySchedule).NotEmpty().WithMessage("Haftalık program belirtilmelidir.");
        RuleFor(x => x.UpdatedBy).NotEmpty().WithMessage("Güncelleyen kişi belirtilmelidir.");
    }
}
