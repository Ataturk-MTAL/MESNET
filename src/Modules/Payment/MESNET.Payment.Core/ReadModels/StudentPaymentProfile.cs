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

    // 3308 Madde 25: MESEM 12. sınıf öğrencisi taban ücreti asgari ücretin %50'si (diğerlerinde
    // işletme büyüklüğüne göre %15/%30). Devlet katkısında ise MESEM öğrencisinin tamamı
    // karşılanıyor, sınıf şartı yok. StudentRegistered her iki bilgiyi de taşıyordu ama
    // consumer atıyordu (#64).
    public int ClassYear { get; set; }

    /// <summary><c>Formal</c> (Örgün) veya <c>Mesem</c> (MESEM).</summary>
    public string EducationTypeName { get; set; } = "";

    /// <summary>
    /// Kalfalık yeterliğini kazandı mı — %50 oranının şartı (3308 Madde 25). Varsayılan false;
    /// eksik veri fazla ödeme üretmesin diye düşük orana düşülür (#83).
    /// </summary>
    public bool HasJourneymanQualification { get; set; }
}
