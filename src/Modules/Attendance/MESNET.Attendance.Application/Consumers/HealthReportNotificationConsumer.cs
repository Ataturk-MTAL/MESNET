using Marten;
using MESNET.Attendance.Application.Helpers;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Notifications;

namespace MESNET.Attendance.Application.Consumers;

/// <summary>
/// Onay bekleyen sağlık raporunu koordinatör öğretmene bildirir (#172).
/// Devamsızlık girişindeki <c>NotifyAttendancePendingApproval</c> deseninin aynısı: onay
/// zincirinin 1. adımı öğretmende olduğu için, bekleyen rapor ona duyurulmazsa zincir durur.
/// </summary>
public static class HealthReportNotificationConsumer
{
    public static async Task Consume(
        HealthReportAttached @event,
        ISseNotificationService notificationService,
        IQuerySession session)
    {
        // Okul tarafının doğrudan girdiği rapor onay beklemez — bildirilecek bir şey yok.
        if (!@event.RequiresApproval) return;

        var placement = await session.Query<InternshipPlacementView>()
            .FirstOrDefaultAsync(p => p.StudentId == @event.StudentId);

        if (placement?.TeacherId is not { } teacherId) return;

        // Ad mesajda taşınmaz, tüketim anında çözülür (#139).
        var attachedByName = await UserNameResolver.ResolveOneAsync(session, @event.AttachedById);

        var notification = new SseNotification(
            EventType: "attendance.health-report-pending",
            Module: "Attendance",
            Payload: new
            {
                attendanceId = @event.AttendanceId,
                studentId = @event.StudentId,
                attachedById = @event.AttachedById,
                attachedBy = attachedByName,
                attachedAt = @event.AttachedAt
            },
            OccurredAt: DateTime.UtcNow);

        await notificationService.PublishAsync(notification, new NotificationTarget { UserIds = [teacherId] });
    }
}
