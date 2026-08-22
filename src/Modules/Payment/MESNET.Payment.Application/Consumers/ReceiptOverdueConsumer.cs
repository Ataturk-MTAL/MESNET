using Marten;
using MESNET.Common.Infrastructure.Notifications;
using MESNET.Common.Shared.Security;
using MESNET.Payment.Application.Messages;
using MESNET.Payment.Core.Entities;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Dekont son günü geldiğinde (ayın 8'i) dekont hâlâ yüklenmemişse ilgilileri uyarır (#69).
///
/// business-rules.md §6.6: işletme her ayın 8'ine kadar ücreti yatırır ve dekontu yükler.
/// Yüklemezse süreç sessizce takılı kalıyordu — öğrencinin fallback yükleme akışı
/// (UploadReceiptByStudent) mevcut ama onu tetikleyen hiçbir mekanizma yoktu.
/// </summary>
public static class ReceiptOverdueConsumer
{
    public static async Task Consume(
        ReceiptOverdue message,
        IQuerySession session,
        ISseNotificationService notifications,
        CancellationToken cancellationToken)
    {
        var summary = await session.LoadAsync<PaymentSummary>(message.SalaryPeriodId, cancellationToken);

        // Maaş dönemi silinmiş/hiç oluşmamışsa yapacak bir şey yok.
        if (summary is null) return;

        // Dekont bu arada yüklendiyse sessizce yut — zamanlanmış mesaj iptal edilemiyor,
        // bu yüzden koşul burada, tetiklenme anında değerlendirilir.
        if (summary.ReceiptId is not null) return;

        // Süreç zaten bitmiş veya reddedilmişse uyarma.
        if (summary.Phase.IsFinal) return;

        var payload = new
        {
            salaryPeriodId = summary.Id,
            studentId = summary.StudentId,
            businessId = summary.BusinessId,
            studentName = summary.StudentName,
            month = summary.Month,
            netAmount = summary.NetAmount,
            dueDate = message.DueDate
        };

        // 1) Öğrenci — kendi dekontunu yükleyebileceği fallback akışına yönlendirilir.
        await notifications.PublishAsync(
            new SseNotification(
                EventType: "payment.receipt-overdue.student",
                Module: "Payment",
                Payload: payload,
                OccurredAt: DateTime.UtcNow),
            new NotificationTarget { StudentIds = [summary.StudentId] },
            cancellationToken);

        // 2) Onay yetkisi olanlar (koordinatör öğretmen, müdür yardımcısı) — gecikme bildirimi.
        // Hedefleme izin üzerinden: Payment modülü koordinatör öğretmenin kimliğini bilmiyor,
        // öğrenci-öğretmen eşleşmesi Coordination modülünde ve modüller arası doğrudan sorgu
        // yasak. İzin bazlı hedefleme bu sınırı aşmadan doğru kitleye ulaşıyor.
        //
        // KURUM DARALTMASI ZORUNLU (#266): izin ölçütü kiracı sınırını korumaz. Daraltma yokken
        // bir okulun dekont gecikmesi, ÖĞRENCİ ADI payload'da olacak şekilde, salary:approve
        // iznine sahip TÜM okulların onaycılarına gidiyordu.
        await notifications.PublishAsync(
            new SseNotification(
                EventType: "payment.receipt-overdue.approver",
                Module: "Payment",
                Payload: payload,
                OccurredAt: DateTime.UtcNow),
            new NotificationTarget
            {
                InstitutionId = summary.InstitutionId,
                RequiredPermission = Permissions.Salary.Approve
            },
            cancellationToken);
    }
}
