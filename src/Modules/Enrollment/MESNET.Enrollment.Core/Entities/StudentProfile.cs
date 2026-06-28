using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Common.Shared.Enums;
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
    public string? PhoneNumber { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public required string BranchCode { get; set; }
    public required string BranchName { get; set; }
    public string? SpecializationCode { get; set; }
    public string? SpecializationName { get; set; }
    public int ClassYear { get; set; }
    public string? Section { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<EducationType, int>))]
    public EducationType EducationType { get; set; } = EducationType.Formal;

    /// <summary>LINQ sorguları için düz string kopyası</summary>
    public string EducationTypeName { get; set; } = EducationType.Formal.Name;

    private StudentStatus _status = StudentStatus.Registered;

    [JsonConverter(typeof(SmartEnumNameConverter<StudentStatus, int>))]
    public StudentStatus Status
    {
        get => _status;
        set { _status = value; StatusName = value.Name; }
    }

    /// <summary>LINQ sorguları için düz string kopyası — Status setter'ı otomatik senkron tutar
    /// (SmartEnum nested-path LINQ tuzağı: s.Status.Name NULL döner).</summary>
    public string StatusName { get; private set; } = StudentStatus.Registered.Name;

    public GuardianInfo? Guardian { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
