using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Email;
using MESNET.Security.Core.Entities;
using Microsoft.Extensions.Logging;

namespace MESNET.Security.Application.Consumers;

/// <summary>
/// Kademeli devamsızlık tebligatının <b>e-posta</b> ayağı (#247, md. 36 (4)).
///
/// <para><b>Neden Security modülünde:</b> alıcı adresleri <c>UserAccount.Email</c>'de, yani
/// Security şemasında. Attendance oraya sorgu <b>atamaz</b> (modül izolasyonu) ve e-posta
/// adresini olayda taşımak kişisel veriyi olay akışına kalıcı yazmak olurdu. Bu tüketici
/// Attendance'ın yalnızca <c>Shared</c> katmanını referans eder — csproj kuralına uygun.</para>
///
/// <para><b>"Yazılı bildirim" gereğini karşılayan kanal budur.</b> Uygulama içi bildirim (SSE)
/// kalıcı değildir: bağlı olmayan kullanıcının bildirimi düşer, sunucuda hiçbir yere yazılmaz ve
/// yeniden bağlanma yoktur.</para>
///
/// <para><b>Üç alıcı grubu, hepsi tek sorgudan:</b> veli (<c>LinkedStudentIds</c> öğrenciyi
/// içerenler), işletme (<c>BusinessId</c> eşleşenler) ve — yalnız <c>NotifyStudent</c> ise —
/// öğrencinin kendisi (<c>StudentId</c> eşleşen). Devre dışı hesaplara gönderilmez.</para>
///
/// <para><b>Alıcı bulunamazsa SESSİZ KALINMAZ.</b> Md. 36 (4) bir yükümlülüktür; alıcı yoksa
/// yükümlülük yerine getirilememiştir ve bunun izi olmalıdır. En yaygın sebep veli bağının hiç
/// kurulmamış olmasıdır — <c>UserAccount.LinkedStudentIds</c>'i dolduran otomatik bir yol
/// yoktur, yalnız <c>POST /api/security/users/{id}/students</c>.</para>
/// </summary>
public static class AbsenceNotificationEmailConsumer
{
    public static async Task Consume(
        AbsenceNotificationDue @event, IQuerySession session, IEmailService email,
        ILogger logger, CancellationToken cancellationToken)
    {
        var stepLabel = $"{@event.Step}. gün";
        var legLabel = @event.Leg == "Unexcused" ? "özürsüz devamsızlık" : "toplam devamsızlık";

        var student = await session.Query<UserAccount>()
            .FirstOrDefaultAsync(u => u.StudentId == @event.StudentId, cancellationToken);

        var studentName = student?.FullName ?? "Öğrenci";

        var recipients = await ResolveRecipientsAsync(session, @event, cancellationToken);

        if (recipients.Count == 0)
        {
            logger.LogWarning(
                "Devamsızlık tebligatı gönderilemedi — HİÇ ALICI BULUNAMADI. Öğrenci: {StudentId}, "
                + "işletme: {BusinessId}, kademe: {Step} ({Leg}). Veli bağı kurulmamış olabilir "
                + "(UserAccount.LinkedStudentIds).",
                @event.StudentId, @event.BusinessId, @event.Step, @event.Leg);
            return;
        }

        foreach (var recipient in recipients)
        {
            var result = await email.SendAbsenceNotificationEmailAsync(
                recipient.Email, recipient.FullName, studentName,
                stepLabel, legLabel, @event.Days, @event.SkippedSteps, cancellationToken);

            if (result.IsFailure)
            {
                logger.LogError(
                    "Devamsızlık tebligatı gönderilemedi: {Email} — öğrenci {StudentId}, {Step}. "
                    + "Hata: {Error}",
                    recipient.Email, @event.StudentId, stepLabel, result.Error.Description);
            }
        }
    }

    /// <summary>
    /// Alıcılar tek sorgudan çözülür ve tekilleştirilir: bir kullanıcı hem veli hem işletme
    /// yetkilisi olabilir; aynı tebligatı iki kez almamalı.
    /// </summary>
    private static async Task<IReadOnlyList<UserAccount>> ResolveRecipientsAsync(
        IQuerySession session, AbsenceNotificationDue @event, CancellationToken ct)
    {
        // Kiracı süzgeci gerekmiyor: UserAccount kiracı damgalıdır ve oturum istek/mesaj
        // bağlamındaki kiracıyla açılır.
        var candidates = await session.Query<UserAccount>()
            .Where(u => u.IsEnabled)
            .ToListAsync(ct);

        return candidates
            .Where(u => !string.IsNullOrWhiteSpace(u.Email))
            .Where(u =>
                // Veli — md. 36 (4) bunu KOŞULSUZ istiyor.
                u.LinkedStudentIds.Contains(@event.StudentId)
                // İşletme — o da koşulsuz. Okulda stajda (#159) işletme yoktur.
                || (@event.BusinessId != Guid.Empty && u.BusinessId == @event.BusinessId)
                // Öğrencinin kendisi — yalnız 18 yaşını doldurmuşsa (ya da tarihi bilinmiyorsa).
                || (@event.NotifyStudent && u.StudentId == @event.StudentId))
            .DistinctBy(u => u.Id)
            .ToList();
    }
}
