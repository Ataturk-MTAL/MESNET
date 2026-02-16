namespace MESNET.Payment.Shared.Events;

public sealed record ReceiptApprovedByDeputy(
    Guid SalaryPeriodId,
    Guid ReceiptId,
    string ApprovedBy,
    DateTime ApprovedAt);
