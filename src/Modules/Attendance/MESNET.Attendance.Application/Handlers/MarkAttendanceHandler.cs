using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;

namespace MESNET.Attendance.Application.Handlers;

public static class MarkAttendanceHandler
{
    public static async Task<(Guid, AttendanceMarked, NotifyAttendancePendingApproval?)> Handle(
        MarkAttendance command, IDocumentSession session, ICurrentUserService currentUser)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(AttendanceErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(AttendanceErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        // Öğrenci-işletme eşleşmesi doğrulaması
        var placement = session.Query<InternshipPlacementView>()
            .FirstOrDefault(p => p.StudentId == command.StudentId
                && p.BusinessId == command.BusinessId);

        if (placement is null)
            throw new DomainException("ATTENDANCE_INVALID_PLACEMENT",
                "Bu öğrenci-işletme eşleşmesi bulunamadı. Devamsızlık girişi yapılamaz.");

        var calendar = session.Query<WorkCalendar>()
            .FirstOrDefault(c => c.InstitutionId == command.InstitutionId && c.Year == command.Date.Year);

        if (calendar?.RestrictedDays.Any(d => d.Date.Date == command.Date.Date) == true)
            throw new DomainException("ATTENDANCE_RESTRICTED_DATE",
                "Bu tarih kısıtlı bir gündür, devamsızlık girişi yapılamaz.");

        if (!AbsenceType.TryFromName(command.AbsenceType, true, out _))
            throw new DomainException("ATTENDANCE_INVALID_ABSENCE_TYPE",
                $"Geçersiz devamsızlık türü: {command.AbsenceType}.");

        var markedBy = currentUser.GetFullName();
        var isBusinessUser = currentUser.IsInRole(MesnetRoles.CompanyManager);
        var initialStatus = isBusinessUser
            ? AttendanceStatus.Pending.Name
            : AttendanceStatus.Recorded.Name;

        var id = Guid.NewGuid();
        var @event = new AttendanceMarked(
            id, command.StudentId, command.BusinessId,
            command.InstitutionId, command.AcademicPeriodId,
            command.Date, command.AbsenceType, markedBy, initialStatus);

        session.Events.StartStream<AttendanceRecord>(id, @event);

        NotifyAttendancePendingApproval? notification = null;
        if (isBusinessUser && placement.TeacherId is not null)
        {
            notification = new NotifyAttendancePendingApproval(
                id, command.StudentId, command.BusinessId,
                command.InstitutionId, placement.TeacherId.Value,
                markedBy, command.Date, command.AbsenceType);
        }

        return (id, @event, notification);
    }
}
