using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class UnassignBusinessFromTeacherHandler
{
    public static async Task Handle(
        UnassignBusinessFromTeacher command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(
            command.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        if (!view.AssignedTeacherId.HasValue)
            throw new DomainException(CoordinationErrors.BusinessNotAssigned(command.BusinessId));

        // Eski slot'taki AssignedBusinessId'yi temizle
        if (view.AssignedPeriodNumber.HasValue && view.AssignedDay != null)
        {
            ClearScheduleSlot(session, view);
        }

        // View alanlarını temizle
        view.AssignedTeacherId = null;
        view.AssignedTeacherName = null;
        view.AssignedHours = 0;
        view.AssignedDay = null;
        view.AssignedPeriodNumber = null;

        session.Store(view);
    }

    private static void ClearScheduleSlot(
        IDocumentSession session,
        BusinessCoordinationView view)
    {
        var schedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == view.AssignedTeacherId!.Value &&
                s.AcademicPeriodId == view.AcademicPeriodId);

        if (schedule is null) return;

        if (!Enum.TryParse<DayOfWeek>(view.AssignedDay, true, out var day)) return;

        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == day);
        var slot = dailySchedule?.Periods.FirstOrDefault(p => p.PeriodNumber == view.AssignedPeriodNumber!.Value);

        if (slot is not null && slot.AssignedBusinessId == view.Id)
        {
            slot.AssignedBusinessId = null;

            var updateEvent = new ScheduleUpdated(
                schedule.Id,
                schedule.WeeklySchedule.Select(d => new DailyScheduleData(
                    d.Day.ToString(),
                    d.Periods.Select(p => new PeriodSlotData(p.PeriodNumber, p.Status.Name, p.CourseName)).ToList()
                )).ToList(),
                "system",
                DateTime.UtcNow);

            session.Events.Append(schedule.Id, updateEvent);
        }
    }
}
