namespace MESNET.Coordination.Application.Queries;

/// <summary>
/// Bir öğretmenin ders programı değişiklik geçmişini getir (event sourcing).
/// teacherId zorunlu, scheduleId ile spesifik bir stream sorgulanır.
/// </summary>
public sealed record GetScheduleHistory(
    Guid TeacherId,
    Guid? ScheduleId = null);
