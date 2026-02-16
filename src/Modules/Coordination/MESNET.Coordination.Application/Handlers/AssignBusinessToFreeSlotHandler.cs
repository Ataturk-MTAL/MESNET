using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class AssignBusinessToFreeSlotHandler
{
    public static async Task<(Result, BusinessAssignedToTeacher?)> Handle(
        AssignBusinessToFreeSlot command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // 1. Semester validation
        if (!AcademicSemester.TryFromName(command.Semester, true, out var semester))
        {
            return (Result.Failure(CoordinationErrors.InvalidSemester(command.Semester)), null);
        }

        // 2. Day validation
        if (!Enum.TryParse<DayOfWeek>(command.Day, true, out var day))
        {
            return (Result.Failure(CoordinationErrors.InvalidDay(command.Day)), null);
        }

        // 3. TeacherSchedule'ı bul
        var schedule = session.Query<TeacherSchedule>()
            .FirstOrDefault(s =>
                s.TeacherId == command.TeacherId &&
                s.AcademicYear == command.AcademicYear &&
                s.Semester == semester);

        if (schedule is null)
        {
            return (Result.Failure(
                CoordinationErrors.ScheduleNotFound(command.TeacherId, command.AcademicYear, command.Semester)), null);
        }

        // 4. İlgili günü bul
        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == day);
        if (dailySchedule is null)
        {
            return (Result.Failure(CoordinationErrors.InvalidDay(command.Day)), null);
        }

        // 5. İlgili period'u bul
        var period = dailySchedule.Periods.FirstOrDefault(p => p.PeriodNumber == command.PeriodNumber);
        if (period is null)
        {
            return (Result.Failure(
                CoordinationErrors.SlotNotFound(command.Day, command.PeriodNumber)), null);
        }

        // 6. Period'un boş olup olmadığını kontrol et
        if (period.Status != SlotStatus.Free)
        {
            return (Result.Failure(
                CoordinationErrors.SlotNotFree(command.Day, command.PeriodNumber)), null);
        }

        // 7. İşletmeyi ata
        period.AssignedBusinessId = command.BusinessId;
        schedule.UpdatedAt = DateTime.UtcNow;

        session.Store(schedule);
        await session.SaveChangesAsync(cancellationToken);

        return (Result.Success(), new BusinessAssignedToTeacher(
            schedule.Id,
            command.TeacherId,
            command.BusinessId,
            command.Day,
            command.PeriodNumber,
            command.AcademicYear,
            command.Semester));
    }
}

/// <summary>
/// Event: Öğretmenin boş saatine işletme atandı
/// </summary>
public sealed record BusinessAssignedToTeacher(
    Guid ScheduleId,
    Guid TeacherId,
    Guid BusinessId,
    string Day,
    int PeriodNumber,
    int AcademicYear,
    string Semester);
