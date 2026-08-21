using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Notifications;

namespace MESNET.Attendance.Application.Consumers;

/// <summary>
/// Kademeli devamsızlık tebligatının <b>uygulama içi</b> teslimatı (#247, md. 36 (4)).
///
/// <para><b>Üç alıcı, üç ayrı hedefleme boyutu:</b> veli (<c>GuardianOfStudentIds</c>), işletme
/// (<c>BusinessIds</c>) ve — yalnız 18 yaşını doldurmuşsa — öğrencinin kendisi
/// (<c>StudentIds</c>). Veli ve işletme ayakları <b>koşulsuzdur</b>; öğrenci ayağı
/// <c>NotifyStudent</c> ile gelir ve doğum tarihi bilinmiyorsa <c>true</c>'dur (#247 2/3).</para>
///
/// <para><b>Boyutlar neden ayrı hedeflerde:</b> hepsi tek bir <c>NotificationTarget</c>'a
/// konabilirdi — ölçütler OR mantığında çalışıyor. Ama <c>StudentIds</c> yalnız 18+ hâlinde
/// eklenmeli; tek hedefte toplanınca "öğrenciyi dâhil etme" kararı ölçütün varlığına bağlı
/// kalır ve okunması zorlaşır. Ayrıca ayrı yayınlar, alıcı grubu başına ayrı
/// <c>EventType</c> taşıyabilir — arayüz veliye ve işletmeye farklı metin gösterebilsin.</para>
///
/// <para><b>SSE bir kolaylıktır, TEBLİGAT KANITI DEĞİLDİR.</b> Bağlı olmayan kullanıcının
/// bildirimi düşer (bounded channel, <c>DropOldest</c>), sunucuda hiçbir yere kalıcı yazılmaz ve
/// yeniden bağlanma yoktur. Md. 36 (4)'ün "yazılı" gereğini karşılayan e-posta ayağıdır; bu
/// tüketici yalnız uygulama içi görünürlük sağlar.</para>
/// </summary>
public static class AbsenceNotificationConsumer
{
    public static async Task Consume(
        AbsenceNotificationDue @event, ISseNotificationService notifications,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            studentId = @event.StudentId,
            businessId = @event.BusinessId,
            academicPeriodId = @event.AcademicPeriodId,
            leg = @event.Leg,
            step = @event.Step,
            days = @event.Days,
            skippedSteps = @event.SkippedSteps,
            occurredAt = @event.OccurredAt
        };

        // Veli — md. 36 (4) bunu KOŞULSUZ istiyor.
        await notifications.PublishAsync(
            Notification("attendance.absence-step.guardian", payload),
            new NotificationTarget { GuardianOfStudentIds = [@event.StudentId] },
            cancellationToken);

        // İşletme — o da koşulsuz. Okulda stajda (#159) işletme yoktur; boş kimlik hedeflenmez.
        if (@event.BusinessId != Guid.Empty)
        {
            await notifications.PublishAsync(
                Notification("attendance.absence-step.business", payload),
                new NotificationTarget { BusinessIds = [@event.BusinessId] },
                cancellationToken);
        }

        // Öğrencinin kendisi — yalnız 18 yaşını doldurmuşsa (ya da doğum tarihi bilinmiyorsa).
        if (@event.NotifyStudent)
        {
            await notifications.PublishAsync(
                Notification("attendance.absence-step.student", payload),
                new NotificationTarget { StudentIds = [@event.StudentId] },
                cancellationToken);
        }
    }

    private static SseNotification Notification(string eventType, object payload) => new(
        EventType: eventType,
        Module: "Attendance",
        Payload: payload,
        OccurredAt: DateTime.UtcNow);
}
