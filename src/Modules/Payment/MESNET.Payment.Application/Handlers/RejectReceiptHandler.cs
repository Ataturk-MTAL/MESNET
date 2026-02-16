using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class RejectReceiptHandler
{
    public static Result<ReceiptRejected> Handle(RejectReceipt command, Guid receiptId)
    {
        return Result<ReceiptRejected>.Success(new ReceiptRejected(
            command.SalaryPeriodId,
            receiptId,
            command.RejectedBy,
            command.Reason
        ));
    }
}
