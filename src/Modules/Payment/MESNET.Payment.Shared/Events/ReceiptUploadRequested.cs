namespace MESNET.Payment.Shared.Events;

public sealed record ReceiptUploadRequested(
    Guid SalaryPeriodId,
    Guid StudentId,
    Guid BusinessId,
    DateTime DueDate);
