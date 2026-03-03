namespace MESNET.Enrollment.Core.Entities;

public class TeacherProfile
{
    public Guid Id { get; set; }
    public Guid KeycloakUserId { get; set; }
    public required string FullName { get; set; }
    public Guid InstitutionId { get; set; }
    public string? BranchCode { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
