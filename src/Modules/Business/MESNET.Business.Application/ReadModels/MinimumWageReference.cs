namespace MESNET.Business.Application.ReadModels;

public class MinimumWageReference
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
