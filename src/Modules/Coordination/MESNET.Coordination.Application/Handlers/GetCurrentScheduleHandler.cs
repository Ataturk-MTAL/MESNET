using Marten;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class GetCurrentScheduleHandler
{
    public static TeacherScheduleDto? Handle(
        GetCurrentSchedule query,
        IQuerySession session)
    {
        if (!AcademicSemester.TryFromName(query.Semester, true, out var semester))
            return null;

        // Seçili dönem + yarıyıl için schedule'ı getir
        var schedule = session.Query<TeacherSchedule>()
            .Where(s =>
                s.TeacherId == query.TeacherId &&
                s.AcademicPeriodId == query.AcademicPeriodId &&
                s.SemesterNumber == semester.Number)
            .FirstOrDefault();

        if (schedule is null) return null;

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
