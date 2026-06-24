namespace MESNET.Payment.Application.Commands;

public sealed record RejectReceipt(
    Guid SalaryPeriodId,
    string RejectedBy,
    string Reason) : ISalaryPeriodScoped;
