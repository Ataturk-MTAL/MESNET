namespace MESNET.Payment.Shared.Events;

public sealed record SalaryCalculated(
    Guid SalaryPeriodId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string Month,
    decimal NetAmount,
    decimal BaseWage,
    decimal Deduction,
    decimal GovContribution);
