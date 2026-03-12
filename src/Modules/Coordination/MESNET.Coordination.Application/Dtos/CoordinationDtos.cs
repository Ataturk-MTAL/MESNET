namespace MESNET.Coordination.Application.Dtos;

// ── Teacher Schedule ──

public sealed record TeacherScheduleDto(
    Guid Id,
    Guid TeacherId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int AcademicYear,
    string Semester,
    List<DailyScheduleDto> WeeklySchedule,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    int Version);

public sealed record DailyScheduleDto(
    string Day,
    List<PeriodSlotDto> Periods);

public sealed record PeriodSlotDto(
    int PeriodNumber,
    string Status,
    string? CourseName,
    Guid? AssignedBusinessId);

// ── Schedule History ──

/// <summary>
/// Bir öğretmenin tüm ders programı stream'lerinin özeti
/// </summary>
public sealed record ScheduleStreamSummaryDto(
    Guid ScheduleId,
    int AcademicYear,
    string Semester,
    int VersionCount,
    DateTime CreatedAt,
    DateTime? LastUpdatedAt,
    string CreatedBy,
    string? LastUpdatedBy);

/// <summary>
/// Bir ders programının değişiklik geçmişi (event listesi)
/// </summary>
public sealed record ScheduleVersionDto(
    int Version,
    string EventType,
    DateTime Timestamp,
    string UpdatedBy,
    List<DailyScheduleDto> WeeklySchedule);

public sealed record ScheduleHistoryDto(
    Guid ScheduleId,
    Guid TeacherId,
    int AcademicYear,
    string Semester,
    int CurrentVersion,
    List<ScheduleVersionDto> Versions);

// ── Business Assignment ──

public sealed record BusinessAssignmentDto(
    Guid BusinessId,
    string BusinessName,
    string? Address,
    string? District,
    double? DistanceToSchoolKm,
    bool IsManualDistance,
    int MaxCoordinationHours,
    int AssignedHours,
    Guid? AssignedTeacherId,
    string? AssignedTeacherName,
    string? AssignedDay,
    int? AssignedPeriodNumber,
    int ActiveStudentCount,
    string BranchCode,
    string BranchName,
    List<AssignedSlotInfoDto> AssignedSlots,
    DateTime? LastModifiedAt = null,
    string? LastModifiedBy = null);

public sealed record AssignmentHistoryEntryDto(
    DateTime Timestamp,
    string Action,
    string PerformedBy,
    string? TeacherName,
    string? SlotDay,
    int? SlotPeriod,
    int? AssignedHours,
    string? Details);

public sealed record AssignedSlotInfoDto(string Day, int PeriodNumber);

public sealed record CoordinationSummaryDto(
    int TotalWorkloadPool,
    int TotalAssignedHours,
    int RemainingHours,
    int TotalMaxHours,
    int AssignedBusinessCount,
    int UnassignedBusinessCount,
    List<TeacherWorkloadSummaryDto> TeacherWorkloads);

public sealed record TeacherWorkloadSummaryDto(
    Guid TeacherId,
    string TeacherName,
    int AssignedHours,
    int BusinessCount);

public sealed record TeacherWorkloadDto(
    Guid TeacherId,
    int TotalAssignedHours,
    int BusinessCount,
    List<TeacherBusinessAssignmentDto> Businesses);

public sealed record TeacherBusinessAssignmentDto(
    Guid BusinessId,
    string BusinessName,
    int AssignedHours,
    string? AssignedDay);

// ── Teacher Overview ──

/// <summary>
/// Tek öğretmenin iş yükü + boş saat özeti (öğretmen sekmesi için)
/// </summary>
public sealed record TeacherOverviewDto(
    Guid TeacherId,
    int TotalAssignedHours,
    int BusinessCount,
    bool ScheduleExists,
    Dictionary<string, int> FreeSlotsByDay,      // gün → boş slot sayısı
    Dictionary<string, int> TotalSlotsByDay,     // gün → toplam serbest slot
    List<TeacherBusinessAssignmentDto> Businesses);

/// <summary>
/// Tüm öğretmenlerin tek satır özeti (öğretmen sekmesi tablosu için)
/// </summary>
public sealed record TeacherSummaryRowDto(
    Guid TeacherId,
    string TeacherName,
    int BusinessCount,
    int AssignedHours,
    bool ScheduleExists,
    Dictionary<string, int> FreeSlotsByDay,       // gün → boş slot sayısı
    Dictionary<string, int> AssignedSlotsByDay);   // gün → atanmış koordinatörlük slot sayısı

// ── Weekly Visit ──

public sealed record WeeklyVisitPlanDto(
    Guid Id,
    Guid AcademicPeriodId,
    int Year,
    int WeekNumber,
    string WeekStartDate,
    string WeekEndDate,
    string Scope,
    Guid? ScopeTeacherId,
    string? ScopeBranchCode,
    int AssignmentCount,
    string GeneratedBy,
    DateTime GeneratedAt);

public sealed record WeeklyVisitAssignmentDto(
    Guid Id,
    Guid PlanId,
    Guid TeacherId,
    string TeacherName,
    Guid BusinessId,
    string BusinessName,
    string BranchCode,
    string BranchName,
    string VisitDate,
    string Day,
    int PeriodCount,
    int WeekNumber);

/// <summary>
/// İşletme kümeleme noktası (harita için)
/// </summary>
public sealed record BusinessClusterDto(
    Guid BusinessId,
    string BusinessName,
    double Latitude,
    double Longitude,
    string? District,
    string BranchCode,
    string BranchName,
    int? ClusterId,                // null = gürültü (outlier)
    string? AssignedTeacherName,
    bool IsAssigned,
    int ActiveStudentCount,
    double? DistanceToSchoolKm,
    int MaxCoordinationHours);     // mesafe formülünden hesaplanan maks saat
