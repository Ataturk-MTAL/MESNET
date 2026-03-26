namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Payment modülünün yerel öğrenci profili — Enrollment.StudentRegistered event'inden beslenir.
/// PaymentSummary'ye öğrenci adı/numarası/alan bilgisi denormalize etmek için kullanılır.
/// </summary>
public class StudentPaymentProfile
{
    public Guid Id { get; set; }       // StudentId
    public string FullName { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public string BranchCode { get; set; } = "";
}
