namespace MESNET.Coordination.Application.Dtos;

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
    int ActiveStudentCount,
    string BranchCode,
    string BranchName);

public sealed record CoordinationSummaryDto(
    int TotalAvailableHours,
    int TotalAssignedHours,
    int RemainingHours,
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
