using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ApproveReceiptByTeacherHandler
{
    public static ReceiptApprovedByTeacher Handle(ApproveReceiptByTeacher command, Guid receiptId)
    {
        return new ReceiptApprovedByTeacher(
            command.SalaryPeriodId,
            receiptId,
            command.ApprovedBy,
            DateTime.UtcNow
        );
    }
}
