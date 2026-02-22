using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class UpsertTeacherScheduleHandler
{
    private static readonly HashSet<string> ValidDays = new()
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"
    };

    public static async Task<TeacherScheduleUpserted> Handle(
        UpsertTeacherSchedule command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // Dönem aktiflik kontrolü
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId, cancellationToken);
        if (period is null) throw new DomainException(CoordinationErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(CoordinationErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        // 1. Kurum ayarlarını oku (günlük ders sayısı)
        var institution = await session.LoadAsync<MESNET.Institution.Core.Entities.Institution>(
            command.InstitutionId, cancellationToken);

        if (institution?.ScheduleConfig is null)
        {
            throw new DomainException(CoordinationErrors.ConfigurationMissing("Kurum ders programı ayarları yapılmamış."));
        }

        var maxPeriods = institution.ScheduleConfig.DailyPeriodCount;

        // 2. Semester validation
        if (!AcademicSemester.TryFromName(command.Semester, true, out var semester))
        {
            throw new DomainException(CoordinationErrors.InvalidSemester(command.Semester));
        }

        // 3. WeeklySchedule validation
        foreach (var dayInput in command.WeeklySchedule)
        {
            // 3a. Day validation
            if (!ValidDays.Contains(dayInput.Day))
            {
                throw new DomainException(CoordinationErrors.InvalidDay(dayInput.Day));
            }

            // 3b. Period count validation
            if (dayInput.Periods.Count != maxPeriods)
            {
                throw new DomainException(CoordinationErrors.InvalidPeriodCount(dayInput.Day, maxPeriods, dayInput.Periods.Count));
            }

            // 3c. Period sequence validation (1, 2, 3, ..., maxPeriods)
            var expectedNumbers = Enumerable.Range(1, maxPeriods).ToList();
            var actualNumbers = dayInput.Periods.Select(p => p.PeriodNumber).OrderBy(n => n).ToList();

            if (!expectedNumbers.SequenceEqual(actualNumbers))
            {
                throw new DomainException(CoordinationErrors.InvalidPeriodSequence(dayInput.Day));
            }

            // 3d. SlotStatus validation
            foreach (var slot in dayInput.Periods)
            {
                if (!SlotStatus.TryFromName(slot.Status, true, out _))
                {
                    throw new DomainException(CoordinationErrors.InvalidSlotStatus(slot.Status));
                }
            }
        }

        // 4. Mevcut schedule'ı ara (upsert logic)
        var existingSchedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == command.TeacherId &&
                s.AcademicYear == command.AcademicYear &&
                s.Semester == semester);

        if (existingSchedule is not null)
        {
            // UPDATE: Mevcut schedule'ı güncelle
            existingSchedule.WeeklySchedule = MapWeeklySchedule(command.WeeklySchedule);
            existingSchedule.UpdatedAt = DateTime.UtcNow;

            session.Store(existingSchedule);
            await session.SaveChangesAsync(cancellationToken);

            return new TeacherScheduleUpserted(
                existingSchedule.Id,
                command.TeacherId,
                command.AcademicYear,
                command.Semester,
                IsNew: false);
        }
        else
        {
            // INSERT: Yeni schedule oluştur
            var newSchedule = new TeacherSchedule
            {
                Id = Guid.NewGuid(),
                TeacherId = command.TeacherId,
                InstitutionId = command.InstitutionId,
                AcademicPeriodId = command.AcademicPeriodId,
                AcademicYear = command.AcademicYear,
                Semester = semester,
                WeeklySchedule = MapWeeklySchedule(command.WeeklySchedule),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.UpdatedBy
            };

            session.Store(newSchedule);
            await session.SaveChangesAsync(cancellationToken);

            return new TeacherScheduleUpserted(
                newSchedule.Id,
                command.TeacherId,
                command.AcademicYear,
                command.Semester,
                IsNew: true);
        }
    }

    private static List<DailySchedule> MapWeeklySchedule(List<DailyScheduleInput> weeklyInput)
    {
        return weeklyInput.Select(dayInput => new DailySchedule
        {
            Day = Enum.Parse<DayOfWeek>(dayInput.Day),
            Periods = dayInput.Periods.Select(p => new PeriodSlot
            {
                PeriodNumber = p.PeriodNumber,
                Status = SlotStatus.FromName(p.Status, ignoreCase: true),
                CourseName = p.CourseName
            }).ToList()
        }).ToList();
    }
}

/// <summary>
/// Event: TeacherSchedule oluşturuldu/güncellendi
/// </summary>
public sealed record TeacherScheduleUpserted(
    Guid ScheduleId,
    Guid TeacherId,
    int AcademicYear,
    string Semester,
    bool IsNew);
