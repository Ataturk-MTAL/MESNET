using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class RejectReceiptHandler
{
    public static ReceiptRejected Handle(RejectReceipt command, Guid receiptId)
    {
        return new ReceiptRejected(command.SalaryPeriodId, receiptId, command.RejectedBy, command.Reason);
    }
}
