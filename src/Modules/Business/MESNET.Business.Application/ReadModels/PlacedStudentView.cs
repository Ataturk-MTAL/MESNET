namespace MESNET.Business.Application.ReadModels;

public class PlacedStudentView
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public DateTime PlacedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? TransferredAt { get; set; }
}
