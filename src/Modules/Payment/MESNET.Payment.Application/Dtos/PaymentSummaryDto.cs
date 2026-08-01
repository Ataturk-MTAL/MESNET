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
    // Tutarın kaç istihdam günü üzerinden hesaplandığı (#154). Ay içi fesihte aynı öğrenci/ay
    // için iki satır görünür; hangisinin neden yarım tutar taşıdığı buradan okunur.
    int EmployedDays,
    string Phase,
    string PhaseSlug,
    Guid? ReceiptId,
    string? ReceiptObjectPath,
    bool UploadedByStudent,
    DateTime? ReceiptDueDate,
    DateTime? StudentConfirmedAt,
    DateTime? TeacherApprovedAt,
    DateTime? DeputyApprovedAt,
    DateTime LastUpdated,
    string StudentName = "",
    string StudentNumber = "",
    string BranchCode = "");
