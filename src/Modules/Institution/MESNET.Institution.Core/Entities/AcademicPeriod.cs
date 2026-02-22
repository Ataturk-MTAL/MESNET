using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Core.Entities;

public class AcademicPeriod
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public required string Name { get; set; }
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<AcademicPeriodStatus, int>))]
    public AcademicPeriodStatus Status { get; set; } = AcademicPeriodStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
}
