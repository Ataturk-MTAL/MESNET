using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Core.Entities;

public sealed class SkillExam
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public int AcademicYear { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<AcademicSemester, int>))]
    public AcademicSemester Semester { get; set; } = AcademicSemester.Fall;

    public DateTime ExamDate { get; set; }
    public int Score { get; set; }
    public List<ExamCriterion> Criteria { get; set; } = [];
    public List<ExamCommitteeMember> CommitteeMembers { get; set; } = [];

    [JsonConverter(typeof(SmartEnumNameConverter<ExamResult, int>))]
    public ExamResult Result { get; set; } = ExamResult.Passed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
