using FluentValidation;
using MESNET.Attendance.Application.Commands;

namespace MESNET.Attendance.Application.Validators;

public class CorrectAttendanceValidator : AbstractValidator<CorrectAttendance>
{
    public CorrectAttendanceValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty().WithMessage("Devamsızlık kaydı belirtilmelidir.");
        RuleFor(x => x.NewAbsenceType).NotEmpty().WithMessage("Yeni devamsızlık türü belirtilmelidir.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Düzeltme gerekçesi belirtilmelidir.");
    }
}
