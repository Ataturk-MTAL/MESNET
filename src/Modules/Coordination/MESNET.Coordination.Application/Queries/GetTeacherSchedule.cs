namespace MESNET.Coordination.Application.Queries;

/// <summary>
/// Öğretmenin haftalık programını getir
/// </summary>
public sealed record GetTeacherSchedule(
    Guid TeacherId,
    int AcademicYear,
    string Semester);
