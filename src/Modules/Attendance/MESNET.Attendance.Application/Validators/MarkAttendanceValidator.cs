using FluentValidation;
using MESNET.Attendance.Application.Commands;

namespace MESNET.Attendance.Application.Validators;

public class MarkAttendanceValidator : AbstractValidator<MarkAttendance>
{
    public MarkAttendanceValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Öğrenci belirtilmelidir.");
        RuleFor(x => x.BusinessId).NotEmpty().WithMessage("İşletme belirtilmelidir.");
        RuleFor(x => x.InstitutionId).NotEmpty().WithMessage("Kurum belirtilmelidir.");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Tarih belirtilmelidir.");
        RuleFor(x => x.AcademicPeriodId).NotEmpty().WithMessage("Eğitim dönemi belirtilmelidir.");
        RuleFor(x => x.AbsenceType).NotEmpty().WithMessage("Devamsızlık türü belirtilmelidir.");
    }
}
