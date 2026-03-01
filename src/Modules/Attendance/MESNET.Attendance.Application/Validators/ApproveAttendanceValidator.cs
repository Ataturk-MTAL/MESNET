using FluentValidation;
using MESNET.Attendance.Application.Commands;

namespace MESNET.Attendance.Application.Validators;

public class ApproveAttendanceValidator : AbstractValidator<ApproveAttendance>
{
    public ApproveAttendanceValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty().WithMessage("Devamsızlık kaydı belirtilmelidir.");
    }
}
