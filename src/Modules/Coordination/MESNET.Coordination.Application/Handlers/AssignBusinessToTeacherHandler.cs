using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Shared.Events;

namespace MESNET.Coordination.Application.Handlers;

public static class AssignBusinessToTeacherHandler
{
    public static async Task<Coordination.Shared.Events.BusinessAssignedToTeacher> Handle(
        AssignBusinessToTeacher command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(
            command.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        // Hedef saat: takdir edilen saat > 0 ise onu kullan, yoksa verilebilir saat
        var targetHours = view.AssignedHours > 0 ? view.AssignedHours : view.MaxCoordinationHours;

        // Mevcut slot sayısı kontrolü — tüm saatler atanmışsa hata
        if (view.AssignedSlots.Count >= targetHours)
        {
            throw new DomainException(
                CoordinationErrors.AllSlotsAssigned(view.AssignedSlots.Count, targetHours));
        }

        // Duplicate slot kontrolü
        if (command.AssignedDay is not null && command.PeriodNumber.HasValue)
        {
            var duplicate = view.AssignedSlots.Any(s =>
                s.Day == command.AssignedDay && s.PeriodNumber == command.PeriodNumber.Value);
            if (duplicate)
            {
                throw new DomainException(
                    CoordinationErrors.SlotAlreadyAssigned(command.AssignedDay, command.PeriodNumber.Value));
            }
        }

        // Öğretmen başına azami koordinatörlük saati kontrolü
        await ValidateTeacherHourLimit(session, command.TeacherId, command.InstitutionId, view.Id, cancellationToken);

        // Ders yükü havuzu kontrolü — ilk atamada takdir edilen saat varsa havuzu aşmasın
        if (view.AssignedSlots.Count == 0 && command.AssignedHours > 0)
        {
            await ValidateWorkloadPool(session, view, command.AssignedHours, cancellationToken);
        }

        // İlk slot → öğretmen bilgisi ve takdir edilen saat set et
        if (view.AssignedSlots.Count == 0)
        {
            view.AssignedTeacherId = command.TeacherId;
            view.AssignedTeacherName = command.TeacherName;

            // Takdir edilen saat henüz girilmemişse, command'dan gelen değeri kullan
            if (view.AssignedHours == 0 && command.AssignedHours > 0)
            {
                view.AssignedHours = command.AssignedHours;
            }
        }

        // Farklı öğretmene atanmışsa hata
        if (view.AssignedTeacherId.HasValue && view.AssignedTeacherId != command.TeacherId)
        {
            throw new DomainException(
                CoordinationErrors.BusinessAlreadyAssignedToAnotherTeacher(command.BusinessId));
        }

        // Slot'u ekle
        if (command.AssignedDay is not null && command.PeriodNumber.HasValue)
        {
            view.AssignedSlots.Add(new AssignedSlotInfo(command.AssignedDay, command.PeriodNumber.Value));
        }

        // Geriye uyumluluk: ilk slot bilgisini eski alanlara da yaz
        if (view.AssignedSlots.Count > 0)
        {
            var firstSlot = view.AssignedSlots[0];
            view.AssignedDay = firstSlot.Day;
            view.AssignedPeriodNumber = firstSlot.PeriodNumber;
        }

        // Audit trail
        var action = view.AssignedSlots.Count == 1 ? "Assigned" : "SlotAdded";
        view.History.Insert(0, new AssignmentHistoryEntry(
            DateTime.UtcNow,
            action,
            command.AssignedBy,
            command.TeacherName,
            command.AssignedDay,
            command.PeriodNumber,
            view.AssignedHours > 0 ? view.AssignedHours : null,
            action == "Assigned"
                ? $"{command.TeacherName} öğretmene atandı"
                : $"{command.AssignedDay} {command.PeriodNumber}. saat eklendi"));
        view.LastModifiedAt = DateTime.UtcNow;
        view.LastModifiedBy = command.AssignedBy;

        session.Store(view);

        // Period bilgisi verilmişse → TeacherSchedule slot'una da businessId ata
        if (command.PeriodNumber.HasValue && command.AssignedDay is not null)
        {
            await AssignToScheduleSlot(
                session, command.TeacherId, view.AcademicPeriodId,
                command.AssignedDay, command.PeriodNumber.Value,
                command.BusinessId, command.AssignedBy, cancellationToken);
        }

        return new Coordination.Shared.Events.BusinessAssignedToTeacher(
            Guid.Empty,
            command.TeacherId,
            command.BusinessId,
            command.AssignedDay ?? string.Empty,
            command.PeriodNumber ?? 0,
            0,
            string.Empty);
    }

    private static async Task ValidateWorkloadPool(
        IDocumentSession session,
        BusinessCoordinationView view,
        int newAssignedHours,
        CancellationToken cancellationToken)
    {
        var workloadConfig = await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == view.InstitutionId &&
                c.BranchCode == view.BranchCode &&
                c.AcademicPeriodId == view.AcademicPeriodId,
                cancellationToken);

        if (workloadConfig is null) return;

        var otherAssigned = await session.Query<BusinessCoordinationView>()
            .Where(b =>
                b.InstitutionId == view.InstitutionId &&
                b.BranchCode == view.BranchCode &&
                b.AcademicPeriodId == view.AcademicPeriodId &&
                b.Id != view.Id)
            .SumAsync(b => b.AssignedHours, cancellationToken);

        var totalAssigned = otherAssigned + newAssignedHours;

        if (totalAssigned > workloadConfig.TotalWorkloadPool)
        {
            throw new DomainException(
                CoordinationErrors.WorkloadPoolExceeded(totalAssigned, workloadConfig.TotalWorkloadPool));
        }
    }

    private static async Task ValidateTeacherHourLimit(
        IDocumentSession session,
        Guid teacherId,
        Guid institutionId,
        Guid currentBusinessId,
        CancellationToken cancellationToken)
    {
        var config = await session.Query<CoordinationConfig>()
            .FirstOrDefaultAsync(c => c.InstitutionId == institutionId, cancellationToken);

        if (config is null) return; // Config yoksa kontrol atlanır

        // Öğretmenin tüm işletmelerdeki mevcut atanmış slot sayısını topla
        var teacherBusinesses = await session.Query<BusinessCoordinationView>()
            .Where(b => b.AssignedTeacherId == teacherId)
            .ToListAsync(cancellationToken);

        var teacherTotalSlots = teacherBusinesses.Sum(b => b.AssignedSlots.Count);

        // +1 çünkü yeni slot eklenmek üzere
        var newTotal = teacherTotalSlots + 1;

        if (newTotal > config.MaxWeeklyExtraHours)
        {
            throw new DomainException(
                CoordinationErrors.TeacherHoursExceedMax(teacherId, newTotal, config.MaxWeeklyExtraHours));
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

        var schedule = await session.Query<TeacherSchedule>()
            .FirstOrDefaultAsync(s =>
                s.TeacherId == teacherId &&
                s.AcademicPeriodId == academicPeriodId,
                cancellationToken);

        if (schedule is null) return;

        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == dayOfWeek);
        var slot = dailySchedule?.Periods.FirstOrDefault(p => p.PeriodNumber == periodNumber);

        if (slot is null || slot.Status != SlotStatus.Free) return;

        slot.AssignedBusinessId = businessId;

        var updateEvent = new ScheduleUpdated(
            schedule.Id,
            schedule.WeeklySchedule.Select(d => new DailyScheduleData(
                d.Day.ToString(),
                d.Periods.Select(p => new PeriodSlotData(p.PeriodNumber, p.Status.Name, p.CourseName, p.AssignedBusinessId)).ToList()
            )).ToList(),
            assignedBy,
            DateTime.UtcNow);

        session.Events.Append(schedule.Id, updateEvent);
    }
}
