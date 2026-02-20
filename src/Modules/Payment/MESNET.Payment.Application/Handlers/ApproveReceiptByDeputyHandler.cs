using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ApproveReceiptByDeputyHandler
{
    public static ReceiptApprovedByDeputy Handle(ApproveReceiptByDeputy command, Guid receiptId)
    {
        return new ReceiptApprovedByDeputy(command.SalaryPeriodId, receiptId, command.ApprovedBy, DateTime.UtcNow);
    }
}
