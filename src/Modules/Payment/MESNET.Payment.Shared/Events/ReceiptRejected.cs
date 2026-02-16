namespace MESNET.Payment.Shared.Events;

public sealed record ReceiptRejected(
    Guid SalaryPeriodId,
    Guid ReceiptId,
    string RejectedBy,
    string Reason);
