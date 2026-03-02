using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class GetAllTeachersOverviewHandler
{
    public static async Task<List<TeacherSummaryRowDto>> Handle(
        GetAllTeachersOverview query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        // Semester validation
        if (!AcademicSemester.TryFromName(query.Semester, true, out var semester))
            throw new DomainException(CoordinationErrors.InvalidSemester(query.Semester));

        // Atanmış işletmeleri öğretmen bazında grupla
        IQueryable<BusinessCoordinationView> viewQuery = session.Query<BusinessCoordinationView>()
            .Where(v =>
                v.InstitutionId == query.InstitutionId &&
                v.AcademicPeriodId == query.AcademicPeriodId &&
                v.AssignedTeacherId != null);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            viewQuery = viewQuery.Where(v => v.BranchCode == query.BranchCode);

        var views = await viewQuery.ToListAsync(cancellationToken);

        // Tüm öğretmenlerin schedule'larını toplu yükle
        var schedules = await session.Query<TeacherSchedule>()
            .Where(s =>
                s.InstitutionId == query.InstitutionId &&
                s.AcademicPeriodId == query.AcademicPeriodId &&
                s.SemesterNumber == semester.Number)
            .ToListAsync(cancellationToken);

        var scheduleByTeacher = schedules.ToDictionary(s => s.TeacherId);

        // Öğretmen bazlı gruplama
        var teacherGroups = views
            .GroupBy(v => new { v.AssignedTeacherId, v.AssignedTeacherName });

        var result = new List<TeacherSummaryRowDto>();

        foreach (var group in teacherGroups)
        {
            if (!group.Key.AssignedTeacherId.HasValue) continue;

            var teacherId = group.Key.AssignedTeacherId.Value;
            var teacherName = group.Key.AssignedTeacherName ?? "—";
            var assignedHours = group.Sum(v => v.AssignedHours);
            var businessCount = group.Count();

            var freeSlotsByDay = new Dictionary<string, int>();
            var scheduleExists = scheduleByTeacher.TryGetValue(teacherId, out var schedule);

            if (schedule is not null)
            {
                foreach (var day in schedule.WeeklySchedule)
                {
                    var dayName = day.Day.ToString();
                    freeSlotsByDay[dayName] = day.Periods.Count(p =>
                        p.Status == SlotStatus.Free && p.AssignedBusinessId is null);
                }
            }

            result.Add(new TeacherSummaryRowDto(
                teacherId,
                teacherName,
                businessCount,
                assignedHours,
                scheduleExists,
                freeSlotsByDay));
        }

        return result.OrderBy(r => r.TeacherName).ToList();
    }
}
