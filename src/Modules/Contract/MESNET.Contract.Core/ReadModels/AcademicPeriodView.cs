namespace MESNET.Contract.Core.ReadModels;

public class AcademicPeriodView
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime? ClosedAt { get; set; }
}
