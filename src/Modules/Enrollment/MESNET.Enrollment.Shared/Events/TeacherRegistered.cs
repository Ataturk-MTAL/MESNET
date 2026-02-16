namespace MESNET.Enrollment.Shared.Events;

public sealed record TeacherRegistered(
    Guid TeacherId,
    string FullName,
    Guid InstitutionId);
