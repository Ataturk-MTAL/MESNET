namespace MESNET.Payment.Application.Commands;

public sealed record ConfirmSalary(
    Guid SalaryPeriodId,
    Guid StudentId) : ISalaryPeriodScoped;
