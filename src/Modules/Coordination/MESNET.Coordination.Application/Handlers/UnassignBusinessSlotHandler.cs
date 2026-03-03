using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Shared.Events;

namespace MESNET.Coordination.Application.Handlers;

public static class UnassignBusinessSlotHandler
{
    public static async Task Handle(
        UnassignBusinessSlot command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(
            command.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        if (!view.AssignedTeacherId.HasValue)
            throw new DomainException(CoordinationErrors.BusinessNotAssigned(command.BusinessId));

        // Slot'u bul ve kaldır
        var slotToRemove = view.AssignedSlots
            .FirstOrDefault(s => s.Day == command.Day && s.PeriodNumber == command.PeriodNumber);

        if (slotToRemove is null)
        {
            throw new DomainException(
                CoordinationErrors.SlotNotAssigned(command.BusinessId, command.Day, command.PeriodNumber));
        }

        view.AssignedSlots.Remove(slotToRemove);

        // TeacherSchedule slot'unu temizle
        ClearScheduleSlot(session, view, command.Day, command.PeriodNumber);

        // Son slot silindiyse → öğretmen atamasını da temizle
        if (view.AssignedSlots.Count == 0)
        {
            view.AssignedTeacherId = null;
            view.AssignedTeacherName = null;
            view.AssignedDay = null;
            view.AssignedPeriodNumber = null;
        }
        else
        {
            // Geriye uyumluluk: ilk slot bilgisini eski alanlara yaz
            var firstSlot = view.AssignedSlots[0];
            view.AssignedDay = firstSlot.Day;
            view.AssignedPeriodNumber = firstSlot.PeriodNumber;
        }

        session.Store(view);
    }

    private static void ClearScheduleSlot(
        IDocumentSession session,
        BusinessCoordinationView view,
        string slotDay,
        int slotPeriodNumber)
    {
        if (!view.AssignedTeacherId.HasValue) return;

        var schedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == view.AssignedTeacherId!.Value &&
                s.AcademicPeriodId == view.AcademicPeriodId);

        if (schedule is null) return;

        if (!Enum.TryParse<DayOfWeek>(slotDay, true, out var day)) return;

        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == day);
        var slot = dailySchedule?.Periods.FirstOrDefault(p => p.PeriodNumber == slotPeriodNumber);

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
