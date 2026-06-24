namespace MESNET.Payment.Application.Commands;

public sealed record ApproveReceiptByDeputy(
    Guid SalaryPeriodId,
    string ApprovedBy) : ISalaryPeriodScoped;
