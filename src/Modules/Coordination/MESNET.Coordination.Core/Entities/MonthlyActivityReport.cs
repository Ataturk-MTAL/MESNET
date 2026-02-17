using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Core.Entities;

public sealed class MonthlyActivityReport
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid TeacherId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<DailyActivity> Activities { get; set; } = [];
    public string? InstructorComment { get; set; }
    public string? TeacherComment { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<ReportStatus, int>))]
    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
