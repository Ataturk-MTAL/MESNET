namespace MESNET.Coordination.Application.Commands;

public sealed record AssignBusinessToTeacher(
    Guid BusinessId,
    Guid TeacherId,
    string TeacherName,
    int AssignedHours,
    string AssignedDay,
    Guid InstitutionId,
    string AssignedBy);
