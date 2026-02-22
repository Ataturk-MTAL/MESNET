using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Application.Commands;

public sealed record CreateMonthlyActivityReport(
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid TeacherId,
    int Year,
    int Month,
    List<DailyActivity> Activities,
    string? InstructorComment,
    string? TeacherComment);

public sealed record UpdateMonthlyActivityReport(
    Guid ReportId,
    List<DailyActivity> Activities,
    string? InstructorComment,
    string? TeacherComment);

public sealed record SubmitMonthlyActivityReport(Guid ReportId);

public sealed record ApproveMonthlyActivityReport(Guid ReportId);
