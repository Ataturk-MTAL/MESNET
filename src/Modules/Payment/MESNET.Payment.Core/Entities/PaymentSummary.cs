using MESNET.Payment.Core.Enums;

namespace MESNET.Payment.Core.Entities;

public class PaymentSummary
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public string Month { get; set; } = default!;
    public decimal BaseWage { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GovernmentContribution { get; set; }
    public decimal EmployerPayment { get; set; }
    public PaymentPhase Phase { get; set; } = PaymentPhase.Calculated;
    public Guid? ReceiptId { get; set; }
    public string? ReceiptObjectPath { get; set; }
    public bool UploadedByStudent { get; set; }
    public DateTime? ReceiptDueDate { get; set; }
    public DateTime? StudentConfirmedAt { get; set; }
    public DateTime? TeacherApprovedAt { get; set; }
    public DateTime? DeputyApprovedAt { get; set; }
    public DateTime LastUpdated { get; set; }

    // Denormalize öğrenci + alan bilgisi (StudentRegistered event'inden)
    public string StudentName { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public string BranchCode { get; set; } = "";
}
