using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ConfirmSalaryHandler
{
    public static Result<SalaryConfirmedByStudent> Handle(ConfirmSalary command)
    {
        return Result<SalaryConfirmedByStudent>.Success(new SalaryConfirmedByStudent(
            command.SalaryPeriodId,
            command.StudentId,
            DateTime.UtcNow
        ));
    }
}
