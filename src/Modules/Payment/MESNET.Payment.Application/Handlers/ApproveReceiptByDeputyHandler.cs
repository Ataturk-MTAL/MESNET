using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ApproveReceiptByDeputyHandler
{
    public static (Result, ReceiptApprovedByDeputy?) Handle(ApproveReceiptByDeputy command, Guid receiptId)
    {
        return (Result.Success(), new ReceiptApprovedByDeputy(
            command.SalaryPeriodId,
            receiptId,
            command.ApprovedBy,
            DateTime.UtcNow
        ));
    }
}
