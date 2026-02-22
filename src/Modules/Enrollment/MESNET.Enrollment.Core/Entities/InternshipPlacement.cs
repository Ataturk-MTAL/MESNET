using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Enrollment.Core.Enums;

namespace MESNET.Enrollment.Core.Entities;

public class InternshipPlacement
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public Guid? TeacherId { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<PlacementStatus, int>))]
    public PlacementStatus Status { get; set; } = PlacementStatus.Matched;

    [JsonConverter(typeof(SmartEnumNameConverter<ApplicationSource, int>))]
    public ApplicationSource Source { get; set; } = ApplicationSource.InstitutionAssignment;

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TransferredAt { get; set; }
    public string? TransferReason { get; set; }
}
