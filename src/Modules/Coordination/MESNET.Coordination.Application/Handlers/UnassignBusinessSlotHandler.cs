using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Security;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Shared.Events;

namespace MESNET.Coordination.Application.Handlers;

public static class UnassignBusinessSlotHandler
{
    public static async Task Handle(
        UnassignBusinessSlot command,
        IDocumentSession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var view = await CoordinationViewLookup.LoadBranchRowAsync(
            session, command.BusinessId, command.BranchCode, command.AcademicPeriodId, cancellationToken);

        if (view is null)
        {
            throw new DomainException(
                CoordinationErrors.BusinessBranchNotFound(command.BusinessId, command.BranchCode));
        }

        // Kapsam çözümlenmiş satırdan okunur (#126) — bkz. BranchScopeGuard.
        BranchScopeGuard.EnsureCanWrite(currentUser, view.BranchCode);

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
        await ClearScheduleSlot(session, view, command.Day, command.PeriodNumber, cancellationToken);

        // Audit trail
        var teacherName = view.AssignedTeacherName;
        var isFullUnassign = view.AssignedSlots.Count == 0;

        // Son slot silindiyse → öğretmen atamasını da temizle
        if (isFullUnassign)
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

        view.History.Insert(0, new AssignmentHistoryEntry(
            DateTime.UtcNow,
            isFullUnassign ? "Unassigned" : "SlotRemoved",
            // Aktör token'dan gelir, istekten DEĞİL (#137).
            currentUser.GetUserId(),
            teacherName,
            command.Day,
            command.PeriodNumber,
            null,
            isFullUnassign
                ? $"{teacherName} öğretmenden atama kaldırıldı"
                : $"{command.Day} {command.PeriodNumber}. saat kaldırıldı"));
        view.LastModifiedAt = DateTime.UtcNow;
        view.LastModifiedById = currentUser.GetUserId();

        session.Store(view);
    }

    private static async Task ClearScheduleSlot(
        IDocumentSession session,
        BusinessCoordinationView view,
        string slotDay,
        int slotPeriodNumber,
        CancellationToken cancellationToken)
    {
        if (!view.AssignedTeacherId.HasValue) return;

        var schedule = await session.Query<TeacherSchedule>()
            .FirstOrDefaultAsync(s =>
                s.TeacherId == view.AssignedTeacherId!.Value &&
                s.AcademicPeriodId == view.AcademicPeriodId,
                cancellationToken);

        if (schedule is null) return;

        if (!Enum.TryParse<DayOfWeek>(slotDay, true, out var day)) return;

        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == day);
        var slot = dailySchedule?.Periods.FirstOrDefault(p => p.PeriodNumber == slotPeriodNumber);

        if (slot is not null && slot.AssignedBusinessId == view.BusinessId)
        {
            slot.AssignedBusinessId = null;

            var updateEvent = new ScheduleUpdated(
                schedule.Id,
                schedule.WeeklySchedule.Select(d => new DailyScheduleData(
                    d.Day.ToString(),
                    d.Periods.Select(p => new PeriodSlotData(p.PeriodNumber, p.Status.Name, p.CourseName, p.AssignedBusinessId)).ToList()
                )).ToList(),
                // Slot temizliği atama kaldırmanın yan etkisidir, ayrı bir kullanıcı
                // eylemi değil — sistem damgası (Guid.Empty) bilinçlidir (#137).
                Guid.Empty,
                DateTime.UtcNow);

            session.Events.Append(schedule.Id, updateEvent);
        }
    }
}
