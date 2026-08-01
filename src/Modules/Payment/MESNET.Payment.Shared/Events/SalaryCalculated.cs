namespace MESNET.Payment.Shared.Events;

// BusinessId / InstitutionId / ReceiptDueDate olayda taşınmalı: PaymentSummary bu üç alanı
// yalnız buradan alabiliyor. Taşınmadığı sürece özet kaydı boş Guid ve null tarihle yazılıyordu,
// işletme/kurum bazlı filtreleme ve son-gün indeksi çalışmıyordu (#74).
public sealed record SalaryCalculated(
    Guid SalaryPeriodId,
    // Dönemin sözleşmesi (#154). Ay içi fesihte aynı öğrenci/ay için birden çok dönem oluşur;
    // özet kaydı hangi sözleşmeye ait olduğunu taşımazsa iki satır ayırt edilemez.
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string Month,
    decimal NetAmount,
    decimal BaseWage,
    decimal Deduction,
    decimal GovContribution,
    DateTime ReceiptDueDate,
    // Tutarın kaç istihdam günü üzerinden hesaplandığı (#154) — "neden yarım ücret" sorusu
    // özet kaydından cevaplanabilmeli.
    int EmployedDays);
