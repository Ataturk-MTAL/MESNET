namespace MESNET.Payment.Shared.Events;

// BusinessId / InstitutionId / ReceiptDueDate olayda taşınmalı: PaymentSummary bu üç alanı
// yalnız buradan alabiliyor. Taşınmadığı sürece özet kaydı boş Guid ve null tarihle yazılıyordu,
// işletme/kurum bazlı filtreleme ve son-gün indeksi çalışmıyordu (#74).
public sealed record SalaryCalculated(
    Guid SalaryPeriodId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string Month,
    decimal NetAmount,
    decimal BaseWage,
    decimal Deduction,
    decimal GovContribution,
    DateTime ReceiptDueDate);
