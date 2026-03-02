using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class AssignBusinessToTeacherHandler
{
    public static async Task Handle(
        AssignBusinessToTeacher command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(
            command.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        // Kısıt 1: TakdirEdilenSaat ≤ VerilebilirSaat (işletme bazında)
        if (command.AssignedHours > view.MaxCoordinationHours)
        {
            throw new DomainException(
                CoordinationErrors.AssignedHoursExceedMax(command.AssignedHours, view.MaxCoordinationHours));
        }

        // Kısıt 2: Toplam dağıtılan saat ≤ ToplamVerilebilirSaat (alan bazında)
        var allViews = await session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == command.InstitutionId && v.BranchCode == view.BranchCode)
            .ToListAsync(cancellationToken);

        var totalAvailable = allViews.Sum(v => v.MaxCoordinationHours);
        var totalAssigned = allViews
            .Where(v => v.Id != command.BusinessId)
            .Sum(v => v.AssignedHours) + command.AssignedHours;

        if (totalAssigned > totalAvailable)
        {
            throw new DomainException(
                CoordinationErrors.TotalAssignedHoursExceedAvailable(totalAssigned, totalAvailable));
        }

        // Eski atama varsa ve period bilgisi varsa → eski slot'u temizle
        if (view.AssignedTeacherId.HasValue && view.AssignedPeriodNumber.HasValue && view.AssignedDay != null)
        {
            await ClearOldSlot(session, view, cancellationToken);
        }

        // Atama yap
        view.AssignedTeacherId = command.TeacherId;
        view.AssignedTeacherName = command.TeacherName;
        view.AssignedHours = command.AssignedHours;
        view.AssignedDay = command.AssignedDay;
        view.AssignedPeriodNumber = command.PeriodNumber;

        session.Store(view);

        // Period bilgisi verilmişse → TeacherSchedule slot'una da businessId ata
        if (command.PeriodNumber.HasValue)
        {
            await AssignToScheduleSlot(
                session, command.TeacherId, view.AcademicPeriodId,
                command.AssignedDay, command.PeriodNumber.Value,
                command.BusinessId, command.AssignedBy, cancellationToken);
        }
    }

    private static async Task ClearOldSlot(
        IDocumentSession session,
        BusinessCoordinationView view,
        CancellationToken cancellationToken)
    {
        var schedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == view.AssignedTeacherId!.Value &&
                s.AcademicPeriodId == view.AcademicPeriodId);

        if (schedule is null) return;

        if (!Enum.TryParse<DayOfWeek>(view.AssignedDay, true, out var oldDay)) return;

        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == oldDay);
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

    private static async Task AssignToScheduleSlot(
        IDocumentSession session,
        Guid teacherId,
        Guid academicPeriodId,
        string day,
        int periodNumber,
        Guid businessId,
        string assignedBy,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DayOfWeek>(day, true, out var dayOfWeek))
            return;

        var schedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == teacherId &&
                s.AcademicPeriodId == academicPeriodId);

        if (schedule is null) return;

        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == dayOfWeek);
        var slot = dailySchedule?.Periods.FirstOrDefault(p => p.PeriodNumber == periodNumber);

        if (slot is null || slot.Status != SlotStatus.Free) return;

        slot.AssignedBusinessId = businessId;

        var updateEvent = new ScheduleUpdated(
            schedule.Id,
            schedule.WeeklySchedule.Select(d => new DailyScheduleData(
                d.Day.ToString(),
                d.Periods.Select(p => new PeriodSlotData(p.PeriodNumber, p.Status.Name, p.CourseName)).ToList()
            )).ToList(),
            assignedBy,
            DateTime.UtcNow);

        session.Events.Append(schedule.Id, updateEvent);
    }
}
