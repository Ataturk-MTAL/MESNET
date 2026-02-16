namespace MESNET.Payment.Shared.Events;

public sealed record PaymentCompleted(
    Guid SalaryPeriodId,
    Guid StudentId,
    string Month,
    decimal Amount);
