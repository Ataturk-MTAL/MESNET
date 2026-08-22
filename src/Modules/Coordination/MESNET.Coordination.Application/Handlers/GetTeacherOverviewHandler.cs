using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class GetTeacherOverviewHandler
{
    private static readonly string[] DayOrder = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

    public static async Task<TeacherOverviewDto> Handle(
        GetTeacherOverview query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        // Semester validation
        if (!AcademicSemester.TryFromName(query.Semester, true, out var semester))
            throw new DomainException(CoordinationErrors.InvalidSemester(query.Semester));

        // Atanmış işletmeler
        var views = await session.Query<BusinessCoordinationView>()
            .Where(v =>
                v.InstitutionId == query.InstitutionId &&
                v.AssignedTeacherId == query.TeacherId &&
                v.AcademicPeriodId == query.AcademicPeriodId)
            .ToListAsync(cancellationToken);

        var businesses = views.Select(v => new TeacherBusinessAssignmentDto(
            v.ResolveBusinessId(), v.Name, v.AssignedHours, v.AssignedDay, v.IsHonoraryVisit)).ToList();

        // Ders programı → boş slot'lar
        var schedule = await session.Query<TeacherSchedule>()
            .FirstOrDefaultAsync(s =>
                s.TeacherId == query.TeacherId &&
                s.AcademicPeriodId == query.AcademicPeriodId &&
                s.SemesterNumber == semester.Number,
                cancellationToken);

        var freeSlotsByDay = new Dictionary<string, int>();
        var totalSlotsByDay = new Dictionary<string, int>();

        if (schedule is not null)
        {
            foreach (var day in schedule.WeeklySchedule)
            {
                var dayName = day.Day.ToString();
                var freeCount = day.Periods.Count(p => p.Status == SlotStatus.Free && p.AssignedBusinessId is null);
                var freePlusAssigned = day.Periods.Count(p => p.Status == SlotStatus.Free);
                freeSlotsByDay[dayName] = freeCount;
                totalSlotsByDay[dayName] = freePlusAssigned;
            }
        }

        return new TeacherOverviewDto(
            query.TeacherId,
            // Fahri ziyaret ek ders ücreti doğurmaz → toplam saate girmez (#115)
            views.Sum(v => v.BillableHours()),
            views.Count,
            views.Count(v => v.IsHonoraryVisit),
            schedule is not null,
            freeSlotsByDay,
            totalSlotsByDay,
            businesses);
    }
}
