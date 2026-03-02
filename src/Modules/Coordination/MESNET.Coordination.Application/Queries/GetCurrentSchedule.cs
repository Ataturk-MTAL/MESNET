namespace MESNET.Coordination.Application.Queries;

/// <summary>
/// Öğretmenin seçili dönem ve yarıyıldaki ders programını getir.
/// AcademicPeriodId + Semester ile filtrelenir.
/// </summary>
public sealed record GetCurrentSchedule(Guid TeacherId, Guid AcademicPeriodId, string Semester);
