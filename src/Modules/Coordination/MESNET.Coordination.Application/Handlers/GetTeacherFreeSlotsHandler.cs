using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class GetTeacherFreeSlotsHandler
{
    public static async Task<List<FreeSlotDto>> Handle(
        GetTeacherFreeSlots query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        // Semester validation
        if (!AcademicSemester.TryFromName(query.Semester, true, out var semester))
        {
            throw new DomainException(CoordinationErrors.InvalidSemester(query.Semester));
        }

        // Schedule'ı bul (snapshot'tan)
        var schedule = await session.Query<TeacherSchedule>()
            .FirstOrDefaultAsync(s =>
                s.TeacherId == query.TeacherId &&
                s.AcademicYear == query.AcademicYear &&
                s.SemesterNumber == semester.Number,
                cancellationToken);

        if (schedule is null)
        {
            throw new DomainException(CoordinationErrors.ScheduleNotFound(query.TeacherId, query.AcademicYear, query.Semester));
        }

        // Boş saatleri filtrele
        var freeSlots = new List<FreeSlotDto>();

        foreach (var day in schedule.WeeklySchedule)
        {
            // Day filter (opsiyonel)
            if (!string.IsNullOrWhiteSpace(query.Day) &&
                !day.Day.ToString().Equals(query.Day, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var period in day.Periods.Where(p => p.Status == SlotStatus.Free))
            {
                freeSlots.Add(new FreeSlotDto(
                    day.Day.ToString(),
                    period.PeriodNumber,
                    period.AssignedBusinessId));
            }
        }

        return freeSlots;
    }
}

public sealed record FreeSlotDto(
    string Day,
    int PeriodNumber,
    Guid? AssignedBusinessId);
