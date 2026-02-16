namespace MESNET.Payment.Core.Entities;

public class SalaryCalculationConfig
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public decimal MinimumWage { get; set; }
    public int PersonnelThreshold { get; set; } = 20;
    public decimal LargeBusinessRate { get; set; } = 0.30m;
    public decimal SmallBusinessRate { get; set; } = 0.15m;
    public decimal MEM12thGradeRate { get; set; } = 0.50m;
    public decimal GovContribSmallNonMEM { get; set; } = 0.6667m;
    public decimal GovContribLargeNonMEM { get; set; } = 0.3333m;
    public decimal GovContribMEM { get; set; } = 1.0m;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string UpdatedBy { get; set; } = default!;
}
