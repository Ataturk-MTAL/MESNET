using MESNET.Common.Shared;

namespace MESNET.Common.Infrastructure.Email;

public interface IEmailService
{
    Task<Result> SendInvitationEmailAsync(
        string toEmail, string fullName, string targetRole,
        Guid invitationId, CancellationToken ct = default);

    /// <summary>
    /// Kademeli devamsızlık bildirimi (#247) — MEB Ortaöğretim Kurumları Yönetmeliği md. 36 (4).
    ///
    /// <para><b>"Yazılı bildirim" gereğini karşılayan kanal budur.</b> Uygulama içi bildirim
    /// (SSE) kalıcı değildir: bağlı olmayan kullanıcının bildirimi düşer, sunucuda hiçbir yere
    /// yazılmaz ve yeniden bağlanma yoktur.</para>
    /// </summary>
    /// <param name="stepLabel">Kademe metni — ör. <c>"25. gün"</c>.</param>
    /// <param name="legLabel">Ayak metni — ör. <c>"özürsüz devamsızlık"</c>.</param>
    /// <param name="skippedSteps">
    /// Zamanında yapılamamış kademeler. Sayaç bir sıçramada birden çok kademe geçmiş olabilir;
    /// boş değilse iletide açıkça belirtilir — eksik tebligat sessizce gizlenmemeli.
    /// </param>
    Task<Result> SendAbsenceNotificationEmailAsync(
        string toEmail, string recipientName, string studentName,
        string stepLabel, string legLabel, int days,
        IReadOnlyList<int> skippedSteps, CancellationToken ct = default);
}
