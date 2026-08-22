using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Helpers;
using MESNET.Common.Infrastructure.Notifications;

namespace MESNET.Attendance.Application.Consumers;

public static class AttendanceNotificationConsumer
{
    public static async Task Consume(
        NotifyAttendancePendingApproval command,
        ISseNotificationService notificationService,
        IQuerySession session)
    {
        // Ad mesajda taşınmaz, tüketim anında çözülür (#139) — kuyrukta beklerken
        // değişen ad bildirime bayat yansımasın. Bilinmiyorsa null gönderilir.
        var markedByName = await UserNameResolver.ResolveOneAsync(session, command.MarkedById);

        var notification = new SseNotification(
            EventType: "attendance.pending-approval",
            Module: "Attendance",
            Payload: new
            {
                attendanceId = command.AttendanceId,
                studentId = command.StudentId,
                businessId = command.BusinessId,
                markedById = command.MarkedById,
                markedBy = markedByName,
                date = command.Date,
                absenceType = command.AbsenceType
            },
            OccurredAt: DateTime.UtcNow);

        var target = new NotificationTarget
        {
            UserIds = [command.CoordinatorTeacherId]
        };

        await notificationService.PublishAsync(notification, target);
    }
}
