using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class GetTeacherScheduleHandler
{
    public static async Task<TeacherScheduleDto> Handle(
        GetTeacherSchedule query,
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

        // Entity → DTO mapping (DayOfWeek enum → string)
        return new TeacherScheduleDto(
            schedule.Id,
            schedule.TeacherId,
            schedule.InstitutionId,
            schedule.AcademicPeriodId,
            schedule.AcademicYear,
            schedule.SemesterName,
            schedule.WeeklySchedule.Select(d => new DailyScheduleDto(
                d.Day.ToString(),
                d.Periods.Select(p => new PeriodSlotDto(
                    p.PeriodNumber,
                    p.Status.Name,
                    p.CourseName,
                    p.AssignedBusinessId
                )).ToList()
            )).ToList(),
            schedule.CreatedAt,
            schedule.UpdatedAt,
            schedule.CreatedBy,
            schedule.Version);
    }
}
