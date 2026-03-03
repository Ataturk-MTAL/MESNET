using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Common.Shared;

namespace MESNET.Business.Core.Entities;

public class Business
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<BusinessStatus, int>))]
    public BusinessStatus Status { get; set; } = BusinessStatus.PendingApproval;

    [JsonConverter(typeof(SmartEnumNameConverter<RegistrationSource, int>))]
    public RegistrationSource Source { get; set; } = RegistrationSource.InstitutionRegistered;

    public int PersonnelCount { get; set; }
    public Location? Location { get; set; }
    public BusinessCapacity Capacity { get; set; } = new();
    public List<BusinessRepresentative> Representatives { get; set; } = [];
    public string? TaxNumber { get; set; }
    public string? SgkNumber { get; set; }
    public string? ActivityField { get; set; }
    public List<string> Sectors { get; set; } = [];
    public MasterInstructorInfo? MasterInstructor { get; set; }
    public List<BusinessDocument> Documents { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool HasAssignedTeacher { get; set; }
}
