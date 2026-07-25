namespace MESNET.Payment.Core.Entities;

public class SalaryCalculationConfig
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public decimal MinimumWage { get; set; }

    /// <summary>
    /// 16 yaşından küçükler için belirlenen asgari ücret (#85). 3308 Madde 25 ve MEB Ortaöğretim
    /// Kurumları Yönetmeliği (6)(a) "YAŞINA UYGUN asgari ücret" diyor. null ise ayrı bir tutar
    /// belirlenmemiş demektir ve yaşa bakılmaksızın <see cref="MinimumWage"/> uygulanır.
    /// </summary>
    public decimal? MinimumWageUnder16 { get; set; }

    /// <summary>Aday çırak ve çırak taban oranı — Madde 25: "yüzde otuzundan aşağı olamaz" (#85).</summary>
    public decimal ApprenticeRate { get; set; } = 0.30m;
    public int PersonnelThreshold { get; set; } = 20;
    public decimal LargeBusinessRate { get; set; } = 0.30m;
    public decimal SmallBusinessRate { get; set; } = 0.15m;
    public decimal MEM12thGradeRate { get; set; } = 0.50m;
    // 3308 Geçici Madde 12 "üçte ikisi" / "üçte biri" diyor — tam kesir. Önceden 0.6667m ve
    // 0.3333m yazılıydı; kırpılmış değer öğrenci başına aylık ~0,22 TL sapma üretiyordu (#83).
    public decimal GovContribSmallNonMEM { get; set; } = 2m / 3m;
    public decimal GovContribLargeNonMEM { get; set; } = 1m / 3m;
    public decimal GovContribMEM { get; set; } = 1.0m;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string UpdatedBy { get; set; } = default!;
}
