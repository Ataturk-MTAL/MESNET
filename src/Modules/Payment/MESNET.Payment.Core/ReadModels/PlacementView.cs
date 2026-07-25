namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Payment modülünün yerel yerleştirme kaydı — Enrollment olaylarından beslenir.
/// Aylık maaş zamanlayıcısı kimler için ödeme açacağını buradan bulur (#63).
/// </summary>
/// <remarks>
/// Maaş devamsızlığa değil aya bağlı hesaplanmalı: devamsızlığı olmayan öğrenci de ücretini
/// almalı. Bunun için "o ay staj yapan öğrenciler" listesi gerekiyor; modüller arası doğrudan
/// DB erişimi yasak olduğu için Enrollment'ın yerleştirme verisi burada denormalize tutulur.
/// </remarks>
public class PlacementView
{
    public Guid Id { get; set; }       // PlacementId
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    /// <summary>Fesih/ayrılma sonrası false — o öğrenci için maaş açılmaz.</summary>
    public bool IsActive { get; set; } = true;
}
