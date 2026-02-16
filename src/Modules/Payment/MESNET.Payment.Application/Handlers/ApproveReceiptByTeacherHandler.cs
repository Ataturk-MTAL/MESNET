using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ApproveReceiptByTeacherHandler
{
    public static Result<ReceiptApprovedByTeacher> Handle(ApproveReceiptByTeacher command, Guid receiptId)
    {
        return Result<ReceiptApprovedByTeacher>.Success(new ReceiptApprovedByTeacher(
            command.SalaryPeriodId,
            receiptId,
            command.ApprovedBy,
            DateTime.UtcNow
        ));
    }
}
