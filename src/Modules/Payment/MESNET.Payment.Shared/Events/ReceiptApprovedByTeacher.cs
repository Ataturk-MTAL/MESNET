namespace MESNET.Payment.Shared.Events;

/// <param name="ApprovedById">
/// Onaylayan kullanıcının kimliği — token'dan gelir, istekten ALINMAZ (#137).
/// Modüller arası olayda ad taşınmaz; her modül adı kendi <c>UserNameView</c>'ından çözer.
/// Eski <c>approvedBy</c> JSON anahtarı (serbest metin ad) bu adla artık okunmaz.
/// </param>
public sealed record ReceiptApprovedByTeacher(
    Guid SalaryPeriodId,
    Guid ReceiptId,
    Guid ApprovedById,
    DateTime ApprovedAt);
