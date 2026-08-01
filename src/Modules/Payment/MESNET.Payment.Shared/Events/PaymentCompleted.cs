namespace MESNET.Payment.Shared.Events;

public sealed record PaymentCompleted(
    Guid SalaryPeriodId,
    Guid StudentId,
    string Month,
    decimal Amount,
    // Sınıf yılı katkı kaydı bu olaydan yazılır (#161). Üç alan da olayda taşınmalı:
    // kayıt "hangi sınıf yılı, hangi akademik dönemde, katkı fiilen alındı mı" sorularının
    // üçüne birden cevap verir. Tüketici bunları profilden okuyamaz — onay ayın sonunda
    // gelir ve öğrencinin profili o an bir sonraki sınıfa geçmiş olabilir.
    Guid AcademicPeriodId = default,
    int ClassYear = 0,
    decimal GovernmentContribution = 0m);
