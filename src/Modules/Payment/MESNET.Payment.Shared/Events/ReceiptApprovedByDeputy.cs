namespace MESNET.Payment.Shared.Events;

/// <param name="ApprovedById">
/// Onaylayan kullanıcının kimliği — token'dan gelir, istekten ALINMAZ (#137).
/// Bkz. <see cref="ReceiptApprovedByTeacher.ApprovedById"/>.
/// </param>
public sealed record ReceiptApprovedByDeputy(
    Guid SalaryPeriodId,
    Guid ReceiptId,
    Guid ApprovedById,
    DateTime ApprovedAt);
