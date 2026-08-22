using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class AssignBusinessToFreeSlotHandler
{
    public static async Task<BusinessAssignedToTeacher> Handle(
        AssignBusinessToFreeSlot command,
        IDocumentSession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        // 1. Semester validation
        if (!AcademicSemester.TryFromName(command.Semester, true, out var semester))
        {
            throw new DomainException(CoordinationErrors.InvalidSemester(command.Semester));
        }

        // 2. Day validation
        if (!Enum.TryParse<DayOfWeek>(command.Day, true, out var day))
        {
            throw new DomainException(CoordinationErrors.InvalidDay(command.Day));
        }

        // 3. TeacherSchedule'ı bul (snapshot'tan)
        var schedule = await session.Query<TeacherSchedule>()
            .FirstOrDefaultAsync(s =>
                s.TeacherId == command.TeacherId &&
                s.AcademicYear == command.AcademicYear &&
                s.SemesterNumber == semester.Number,
                cancellationToken);

        if (schedule is null)
        {
            throw new DomainException(CoordinationErrors.ScheduleNotFound(command.TeacherId, command.AcademicYear, command.Semester));
        }

        // 4. İlgili günü bul
        var dailySchedule = schedule.WeeklySchedule.FirstOrDefault(d => d.Day == day);
        if (dailySchedule is null)
        {
            throw new DomainException(CoordinationErrors.InvalidDay(command.Day));
        }

        // 5. İlgili period'u bul
        var periodSlot = dailySchedule.Periods.FirstOrDefault(p => p.PeriodNumber == command.PeriodNumber);
        if (periodSlot is null)
        {
            throw new DomainException(CoordinationErrors.SlotNotFound(command.Day, command.PeriodNumber));
        }

        // 6. Period'un boş olup olmadığını kontrol et
        if (periodSlot.Status != SlotStatus.Free)
        {
            throw new DomainException(CoordinationErrors.SlotNotFree(command.Day, command.PeriodNumber));
        }

        // 7. Önce slot'a businessId ata, sonra event data oluştur
        periodSlot.AssignedBusinessId = command.BusinessId;

        var updatedWeekly = schedule.WeeklySchedule.Select(d => new DailyScheduleData(
            d.Day.ToString(),
            d.Periods.Select(p => new PeriodSlotData(
                p.PeriodNumber,
                p.Status.Name,
                p.CourseName,
                p.AssignedBusinessId
            )).ToList()
        )).ToList();

        // Schedule updated event append
        // Aktör token'dan gelir, istekten DEĞİL (#137).
        var updateEvent = new ScheduleUpdated(
            schedule.Id,
            updatedWeekly,
            currentUser.GetUserId(),
            DateTime.UtcNow);

        session.Events.Append(schedule.Id, updateEvent);

        return new BusinessAssignedToTeacher(
            schedule.Id,
            command.TeacherId,
            command.BusinessId,
            command.Day,
            command.PeriodNumber,
            command.AcademicYear,
            command.Semester);
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
