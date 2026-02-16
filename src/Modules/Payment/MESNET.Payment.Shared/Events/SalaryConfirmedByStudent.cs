namespace MESNET.Payment.Shared.Events;

public sealed record SalaryConfirmedByStudent(
    Guid SalaryPeriodId,
    Guid StudentId,
    DateTime ConfirmedAt);
