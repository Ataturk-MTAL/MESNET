using Marten;
using MESNET.Attendance.Application.Helpers;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Notifications;

namespace MESNET.Attendance.Application.Consumers;

/// <summary>
/// Resmîleşen ücretli izni koordinatör öğretmene bildirir (#177).
///
/// <para><b>Bildirim, onay değil.</b> Sahibin kararı: onay müdür yardımcısı ve müdürdedir;
/// koordinatör öğretmen zincirde adım TUTMAZ. Ama öğrencisinin işletmede olmayacağı günleri
/// bilmesi gerekir — ziyaret planı ve devam takibi ona bağlıdır.</para>
///
/// <para>Desen <c>HealthReportNotificationConsumer</c> ile aynı.</para>
/// </summary>
public static class PaidLeaveNotificationConsumer
{
    public static async Task Consume(
        PaidLeaveApproved @event,
        ISseNotificationService notificationService,
        IQuerySession session)
    {
        var placement = await session.Query<InternshipPlacementView>()
            .FirstOrDefaultAsync(p => p.StudentId == @event.StudentId
                && p.AcademicPeriodId == @event.AcademicPeriodId);

        if (placement?.TeacherId is not { } teacherId) return;

        // Ad mesajda taşınmaz, tüketim anında çözülür (#139).
        var approvedByName = await UserNameResolver.ResolveOneAsync(session, @event.ApprovedById);

        var notification = new SseNotification(
            EventType: "attendance.paid-leave-approved",
            Module: "Attendance",
            Payload: new
            {
                requestId = @event.RequestId,
                studentId = @event.StudentId,
                businessId = @event.BusinessId,
                startDate = @event.StartDate,
                endDate = @event.EndDate,
                reason = @event.Reason,
                approvedById = @event.ApprovedById,
                approvedBy = approvedByName,
                approvedAt = @event.ApprovedAt
            },
            OccurredAt: DateTime.UtcNow);

        await notificationService.PublishAsync(
            notification, new NotificationTarget { UserIds = [teacherId] });
    }
}
