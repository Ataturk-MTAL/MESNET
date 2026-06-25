using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Shared.Events;

namespace MESNET.Coordination.Application.Handlers;

public static class UnassignBusinessFromTeacherHandler
{
    public static async Task<BusinessUnassignedFromTeacher> Handle(
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

        // Tüm slot'ları temizle (multi-slot)
        foreach (var assignedSlot in view.AssignedSlots)
        {
            await ClearScheduleSlot(session, view, assignedSlot.Day, assignedSlot.PeriodNumber, cancellationToken);
        }

        // Eski tek slot alanları (geriye uyumluluk fallback — AssignedSlots boş ama eski alanlar dolu)
        if (view.AssignedSlots.Count == 0 && view.AssignedPeriodNumber.HasValue && view.AssignedDay != null)
        {
            await ClearScheduleSlot(session, view, view.AssignedDay, view.AssignedPeriodNumber.Value, cancellationToken);
        }

        // Audit trail
        var teacherName = view.AssignedTeacherName;
        var slotCount = view.AssignedSlots.Count;

        // View alanlarını temizle
        view.AssignedTeacherId = null;
        view.AssignedTeacherName = null;
        view.AssignedSlots.Clear();
        view.AssignedDay = null;
        view.AssignedPeriodNumber = null;

        view.History.Insert(0, new AssignmentHistoryEntry(
            DateTime.UtcNow,
            "Unassigned",
            command.UnassignedBy,
            teacherName,
            null,
            null,
            null,
            $"{teacherName} öğretmenden tüm atama kaldırıldı ({slotCount} slot)"));
        view.LastModifiedAt = DateTime.UtcNow;
        view.LastModifiedBy = command.UnassignedBy;

        session.Store(view);

        return new BusinessUnassignedFromTeacher(command.BusinessId);
    }

    private static async Task ClearScheduleSlot(
        IDocumentSession session,
        BusinessCoordinationView view,
        string slotDay,
        int slotPeriodNumber,
        CancellationToken cancellationToken)
    {
        var schedule = await session.Query<TeacherSchedule>()
            .FirstOrDefaultAsync(s =>
                s.TeacherId == view.AssignedTeacherId!.Value &&
                s.AcademicPeriodId == view.AcademicPeriodId,
                cancellationToken);

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
                    d.Periods.Select(p => new PeriodSlotData(p.PeriodNumber, p.Status.Name, p.CourseName, p.AssignedBusinessId)).ToList()
                )).ToList(),
                "system",
                DateTime.UtcNow);

            session.Events.Append(schedule.Id, updateEvent);
        }
    }
}
