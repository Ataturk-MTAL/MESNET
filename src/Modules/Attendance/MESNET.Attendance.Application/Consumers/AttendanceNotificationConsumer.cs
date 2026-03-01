using MESNET.Attendance.Application.Commands;
using MESNET.Common.Infrastructure.Notifications;

namespace MESNET.Attendance.Application.Consumers;

public static class AttendanceNotificationConsumer
{
    public static async Task Consume(
        NotifyAttendancePendingApproval command,
        ISseNotificationService notificationService)
    {
        var notification = new SseNotification(
            EventType: "attendance.pending-approval",
            Module: "Attendance",
            Payload: new
            {
                attendanceId = command.AttendanceId,
                studentId = command.StudentId,
                businessId = command.BusinessId,
                markedBy = command.MarkedByName,
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
