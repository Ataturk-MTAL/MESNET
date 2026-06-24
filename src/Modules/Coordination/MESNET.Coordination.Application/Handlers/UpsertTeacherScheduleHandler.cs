using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Shared.Events;

namespace MESNET.Coordination.Application.Handlers;

public static class UpsertTeacherScheduleHandler
{
    private static readonly HashSet<string> ValidDays = new()
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"
    };

    public static async Task<(Guid, TeacherScheduleUpserted)> Handle(
        UpsertTeacherSchedule command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // Dönem aktiflik kontrolü
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId, cancellationToken);
        if (period is null) throw new DomainException(CoordinationErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(CoordinationErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        // 1. Kurum ayarlarını oku (günlük ders sayısı) — event-tabanlı InstitutionView
        var institution = await session.LoadAsync<InstitutionView>(
            command.InstitutionId, cancellationToken);

        if (institution is null || institution.DailyPeriodCount <= 0)
        {
            throw new DomainException(CoordinationErrors.ConfigurationMissing("Kurum ders programı ayarları yapılmamış."));
        }

        var maxPeriods = institution.DailyPeriodCount;

        // 2. Semester validation
        if (!AcademicSemester.TryFromName(command.Semester, true, out var semester))
        {
            throw new DomainException(CoordinationErrors.InvalidSemester(command.Semester));
        }

        // 3. WeeklySchedule validation
        ValidateWeeklySchedule(command.WeeklySchedule, maxPeriods);

        // 4. Event payload hazırla
        var weeklyData = command.WeeklySchedule.Select(d => new DailyScheduleData(
            d.Day,
            d.Periods.Select(p => new PeriodSlotData(p.PeriodNumber, p.Status, p.CourseName, null)).ToList()
        )).ToList();

        // 5. Mevcut schedule'ı ara — event sourcing snapshot'tan
        //    AcademicPeriodId + TeacherId + Semester ile arama (en güvenilir)
        var existingSchedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == command.TeacherId &&
                s.AcademicPeriodId == command.AcademicPeriodId &&
                s.SemesterNumber == semester.Number);

        if (existingSchedule is not null)
        {
            // UPDATE: Mevcut stream'e yeni event ekle
            var updateEvent = new ScheduleUpdated(
                existingSchedule.Id,
                weeklyData,
                command.UpdatedBy,
                DateTime.UtcNow);

            session.Events.Append(existingSchedule.Id, updateEvent);

            var cascading = new TeacherScheduleUpserted(
                existingSchedule.Id, command.TeacherId, command.AcademicYear,
                command.Semester, IsNew: false);

            return (existingSchedule.Id, cascading);
        }
        else
        {
            // CREATE: Yeni event stream başlat
            var scheduleId = Guid.NewGuid();
            var createEvent = new ScheduleCreated(
                scheduleId,
                command.TeacherId,
                command.InstitutionId,
                command.AcademicPeriodId,
                command.AcademicYear,
                command.Semester,
                weeklyData,
                command.UpdatedBy,
                DateTime.UtcNow);

            session.Events.StartStream<TeacherSchedule>(scheduleId, createEvent);

            var cascading = new TeacherScheduleUpserted(
                scheduleId, command.TeacherId, command.AcademicYear,
                command.Semester, IsNew: true);

            return (scheduleId, cascading);
        }
    }

    private static void ValidateWeeklySchedule(List<DailyScheduleInput> weeklyInput, int maxPeriods)
    {
        foreach (var dayInput in weeklyInput)
        {
            if (!ValidDays.Contains(dayInput.Day))
            {
                throw new DomainException(CoordinationErrors.InvalidDay(dayInput.Day));
            }

            if (dayInput.Periods.Count != maxPeriods)
            {
                throw new DomainException(CoordinationErrors.InvalidPeriodCount(dayInput.Day, maxPeriods, dayInput.Periods.Count));
            }

            var expectedNumbers = Enumerable.Range(1, maxPeriods).ToList();
            var actualNumbers = dayInput.Periods.Select(p => p.PeriodNumber).OrderBy(n => n).ToList();

            if (!expectedNumbers.SequenceEqual(actualNumbers))
            {
                throw new DomainException(CoordinationErrors.InvalidPeriodSequence(dayInput.Day));
            }

            foreach (var slot in dayInput.Periods)
            {
                if (!SlotStatus.TryFromName(slot.Status, true, out _))
                {
                    throw new DomainException(CoordinationErrors.InvalidSlotStatus(slot.Status));
                }
            }
        }
    }
}
