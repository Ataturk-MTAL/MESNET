namespace MESNET.Payment.Application.Messages;

/// <summary>
/// Dekont son gününde (ayın 8'i, 23:59:59) tetiklenen zamanlanmış mesaj (#69).
/// Maaş dönemi açılırken Wolverine'e <c>ScheduledAt</c> ile bırakılır.
///
/// Saga timeout'u olarak DEĞİL, ayrı bir tüketici olarak işlenir: saga tamamlandığında
/// (<c>MarkCompleted</c>) Wolverine saga belgesini siliyor, geride kalan zamanlanmış mesaj
/// korele olacak saga bulamayıp dead letter'a düşerdi. Tüketici bunun yerine PaymentSummary
/// okuyup dekontun gelip gelmediğine bakar.
/// </summary>
public sealed record ReceiptOverdue(
    Guid SalaryPeriodId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    string Month,
    DateTime DueDate);
