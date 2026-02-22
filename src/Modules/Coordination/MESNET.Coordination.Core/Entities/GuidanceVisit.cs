using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Core.Entities;

public sealed class GuidanceVisit
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public DateTime VisitDate { get; set; }
    public List<StudentVisitNote> StudentNotes { get; set; } = [];
    public string? InstructorMeetingNotes { get; set; }
    public string? IssuesIdentified { get; set; }
    public string? ActionsTaken { get; set; }
    public string? GeneralAssessment { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<VisitStatus, int>))]
    public VisitStatus Status { get; set; } = VisitStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
