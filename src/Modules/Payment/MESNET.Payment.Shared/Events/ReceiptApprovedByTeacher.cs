namespace MESNET.Payment.Shared.Events;

public sealed record ReceiptApprovedByTeacher(
    Guid SalaryPeriodId,
    Guid ReceiptId,
    string ApprovedBy,
    DateTime ApprovedAt);
