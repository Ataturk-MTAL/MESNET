using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.ValueObjects;

namespace MESNET.Enrollment.Core.Entities;

public class StudentProfile
{
    public Guid Id { get; set; }
    public Guid KeycloakUserId { get; set; }
    public required string FullName { get; set; }
    public string? TcKimlikNo { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? BirthPlace { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? StudentNumber { get; set; }
    public Guid InstitutionId { get; set; }
    public required string BranchCode { get; set; }
    public required string BranchName { get; set; }
    public int ClassYear { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<StudentStatus, int>))]
    public StudentStatus Status { get; set; } = StudentStatus.Registered;

    public GuardianInfo? Guardian { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
