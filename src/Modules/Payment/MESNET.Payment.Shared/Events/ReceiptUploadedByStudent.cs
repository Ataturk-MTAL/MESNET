namespace MESNET.Payment.Shared.Events;

public sealed record ReceiptUploadedByStudent(
    Guid SalaryPeriodId,
    Guid ReceiptId,
    string ObjectPath,
    DateTime UploadedAt);
