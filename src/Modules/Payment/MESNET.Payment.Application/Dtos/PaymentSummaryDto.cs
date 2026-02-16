namespace MESNET.Payment.Application.Dtos;

public sealed record PaymentSummaryDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    string Month,
    decimal BaseWage,
    decimal DeductionAmount,
    decimal NetAmount,
    decimal GovernmentContribution,
    decimal EmployerPayment,
    string Phase,
    string PhaseSlug,
    Guid? ReceiptId,
    string? ReceiptObjectPath,
    bool UploadedByStudent,
    DateTime? ReceiptDueDate,
    DateTime? StudentConfirmedAt,
    DateTime? TeacherApprovedAt,
    DateTime? DeputyApprovedAt,
    DateTime LastUpdated);
