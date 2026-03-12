namespace MESNET.Coordination.Shared.Events;

/// <summary>
/// Haftalık ziyaret planı oluşturulduğunda yayınlanır.
/// Reporting modülü bu eventi dinleyerek Form 3 (Günlük Rehberlik Formu) PDF'lerini otomatik üretir.
/// </summary>
public sealed record WeeklyVisitsGenerated(
    Guid PlanId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    List<WeeklyVisitAssignmentInfo> Assignments);

public sealed record WeeklyVisitAssignmentInfo(
    Guid AssignmentId,
    Guid TeacherId,
    string TeacherName,
    Guid BusinessId,
    string BusinessName,
    string? BusinessAddress,
    string BranchCode,
    string BranchName,
    DateOnly VisitDate,
    int PeriodCount);
