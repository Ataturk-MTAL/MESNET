using MESNET.Common.Infrastructure.Notifications;
using MESNET.Reporting.Application.Commands;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Reporting modülü notification consumer'ları.
/// Wolverine handler pattern — cascading message olarak gelen notification komutlarını işler.
/// </summary>
public static class DocumentNotificationConsumer
{
    public static async Task Consume(
        NotifyDocumentGenerated command,
        ISseNotificationService notificationService)
    {
        var notification = new SseNotification(
            EventType: "document.generated",
            Module: "Reporting",
            Payload: new
            {
                documentId = command.DocumentId,
                formType = command.FormType,
                formTypeSlug = command.FormTypeSlug,
                generatedBy = command.GeneratedByName
            },
            OccurredAt: DateTime.UtcNow);

        var target = BuildTarget(command.InstitutionId, command.TeacherId, command.StudentId);
        await notificationService.PublishAsync(notification, target);
    }

    public static async Task Consume(
        NotifyDocumentStatusChanged command,
        ISseNotificationService notificationService)
    {
        var notification = new SseNotification(
            EventType: "document.status-changed",
            Module: "Reporting",
            Payload: new
            {
                documentId = command.DocumentId,
                formType = command.FormType,
                newStatus = command.NewStatus,
                newStatusSlug = command.NewStatusSlug,
                changedBy = command.ChangedByName
            },
            OccurredAt: DateTime.UtcNow);

        var target = BuildTarget(command.InstitutionId, command.TeacherId, command.StudentId);
        await notificationService.PublishAsync(notification, target);
    }

    public static async Task Consume(
        NotifyDocumentDeleted command,
        ISseNotificationService notificationService)
    {
        var notification = new SseNotification(
            EventType: "document.deleted",
            Module: "Reporting",
            Payload: new
            {
                documentId = command.DocumentId,
                formType = command.FormType,
                deletedBy = command.DeletedByName
            },
            OccurredAt: DateTime.UtcNow);

        var target = BuildTarget(command.InstitutionId, command.TeacherId, null);
        await notificationService.PublishAsync(notification, target);
    }

    private static NotificationTarget BuildTarget(Guid? institutionId, Guid? teacherId, Guid? studentId)
    {
        var userIds = new List<Guid>();
        if (teacherId.HasValue) userIds.Add(teacherId.Value);
        if (studentId.HasValue) userIds.Add(studentId.Value);

        return new NotificationTarget
        {
            UserIds = userIds.Count > 0 ? userIds : null,
            InstitutionId = institutionId,
            Roles = ["InstitutionManager"]
        };
    }
}
