using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Handlers;

public static class MarkAttendanceHandler
{
    public static async Task<(Guid, AttendanceMarked)> Handle(MarkAttendance command, IDocumentSession session)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(AttendanceErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(AttendanceErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        var calendar = session.Query<WorkCalendar>()
            .FirstOrDefault(c => c.InstitutionId == command.InstitutionId && c.Year == command.Date.Year);

        if (calendar?.RestrictedDays.Any(d => d.Date.Date == command.Date.Date) == true)
            throw new DomainException("ATTENDANCE_RESTRICTED_DATE",
                "Bu tarih kısıtlı bir gündür, devamsızlık girişi yapılamaz.");

        if (!AbsenceType.TryFromName(command.AbsenceType, true, out _))
            throw new DomainException("ATTENDANCE_INVALID_ABSENCE_TYPE",
                $"Geçersiz devamsızlık türü: {command.AbsenceType}.");

        var id = Guid.NewGuid();
        var @event = new AttendanceMarked(
            id, command.StudentId, command.BusinessId,
            command.InstitutionId, command.AcademicPeriodId, command.Date, command.AbsenceType);

        session.Events.StartStream<AttendanceRecord>(id, @event);
        return (id, @event);
    }
}
