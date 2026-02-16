using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ConfirmSalaryHandler
{
    public static SalaryConfirmedByStudent Handle(ConfirmSalary command)
    {
        return new SalaryConfirmedByStudent(
            command.SalaryPeriodId,
            command.StudentId,
            DateTime.UtcNow
        );
    }
}
