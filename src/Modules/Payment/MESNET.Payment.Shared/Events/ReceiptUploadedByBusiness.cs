namespace MESNET.Payment.Shared.Events;

public sealed record ReceiptUploadedByBusiness(
    Guid SalaryPeriodId,
    Guid ReceiptId,
    string ObjectPath,
    string UploadedBy,
    DateTime UploadedAt);
