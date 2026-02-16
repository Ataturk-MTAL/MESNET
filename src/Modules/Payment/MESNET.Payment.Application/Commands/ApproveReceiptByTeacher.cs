namespace MESNET.Payment.Application.Commands;

public sealed record ApproveReceiptByTeacher(
    Guid SalaryPeriodId,
    string ApprovedBy);
