using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class GetTeacherScheduleHandler
{
    public static Result<TeacherSchedule> Handle(
        GetTeacherSchedule query,
        IQuerySession session)
    {
        // Semester validation
        if (!AcademicSemester.TryFromName(query.Semester, true, out var semester))
        {
            return Result<TeacherSchedule>.Failure(
                CoordinationErrors.InvalidSemester(query.Semester));
        }

        // Schedule'ı bul
        var schedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == query.TeacherId &&
                s.AcademicYear == query.AcademicYear &&
                s.Semester == semester);

        if (schedule is null)
        {
            return Result<TeacherSchedule>.Failure(
                CoordinationErrors.ScheduleNotFound(query.TeacherId, query.AcademicYear, query.Semester));
        }

        return Result<TeacherSchedule>.Success(schedule);
    }
}
