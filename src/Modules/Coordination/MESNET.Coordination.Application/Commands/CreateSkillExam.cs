using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Application.Commands;

public sealed record CreateSkillExam(
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int AcademicYear,
    string Semester,
    DateTime ExamDate,
    int Score,
    List<ExamCriterion> Criteria,
    List<ExamCommitteeMember> CommitteeMembers,
    string Result);

public sealed record UpdateSkillExam(
    Guid ExamId,
    DateTime ExamDate,
    int Score,
    List<ExamCriterion> Criteria,
    List<ExamCommitteeMember> CommitteeMembers,
    string Result);
