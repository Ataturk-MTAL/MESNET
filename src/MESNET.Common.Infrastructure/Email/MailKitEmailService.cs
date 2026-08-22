using MESNET.Common.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace MESNET.Common.Infrastructure.Email;

public sealed class MailKitEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailKitEmailService> _logger;
    private readonly IEmailTemplateService _templateService;

    public MailKitEmailService(
        IConfiguration configuration,
        ILogger<MailKitEmailService> logger,
        IEmailTemplateService templateService)
    {
        _configuration = configuration;
        _logger = logger;
        _templateService = templateService;
    }

    public async Task<Result> SendInvitationEmailAsync(
        string toEmail, string fullName, string targetRole,
        Guid invitationId, CancellationToken ct = default)
    {
        try
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
            var registrationLink = $"{frontendUrl}/register?token={invitationId}";

            var smtpHost = _configuration["SmtpSettings:Host"] ?? "localhost";
            var smtpPort = int.TryParse(_configuration["SmtpSettings:Port"], out var port) ? port : 1025;
            var smtpUser = _configuration["SmtpSettings:Username"];
            var smtpPass = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? "noreply@mesnet.local";
            var fromName = _configuration["SmtpSettings:FromName"] ?? "MESNET Sistemi";

            var htmlBody = _templateService.RenderInvitation(fullName, targetRole, registrationLink);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(fullName, toEmail));
            message.Subject = "MESNET — Sisteme Kayıt Davetiyesi";

            var builder = new BodyBuilder();

            // Logo'yu CID ile gömülü kaynak olarak ekle
            var logoBytes = _templateService.GetLogoBytes();
            if (logoBytes.Length > 0)
            {
                var logoAttachment = builder.LinkedResources.Add("logo.png", logoBytes, new ContentType("image", "png"));
                logoAttachment.ContentId = "mesnet-logo";
                logoAttachment.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            }

            builder.HtmlBody = htmlBody;
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            // Dev ortamda SSL yok (Mailpit), prod'da TLS var
            var useSsl = !string.IsNullOrEmpty(smtpUser);
            await client.ConnectAsync(
                smtpHost, smtpPort,
                useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                ct);

            if (!string.IsNullOrEmpty(smtpUser))
                await client.AuthenticateAsync(smtpUser, smtpPass ?? string.Empty, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Davet e-postası gönderildi: {Email} ({Role})", toEmail, targetRole);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Davet e-postası gönderilemedi: {Email}", toEmail);
            return Result.Failure(new Error("Security.EmailFailed", $"E-posta gönderilemedi: {ex.Message}"));
        }
    }

    /// <summary>
    /// Kademeli devamsızlık bildirimi (#247) — md. 36 (4)'ün "yazılı bildirim" gereğini karşılayan
    /// kanal. Uygulama içi bildirim (SSE) kalıcı olmadığı için tebligat kanıtı olamaz.
    /// </summary>
    public async Task<Result> SendAbsenceNotificationEmailAsync(
        string toEmail, string recipientName, string studentName,
        string stepLabel, string legLabel, int days,
        IReadOnlyList<int> skippedSteps, CancellationToken ct = default)
    {
        try
        {
            var htmlBody = _templateService.RenderAbsenceNotification(
                recipientName, studentName, stepLabel, legLabel, days, skippedSteps);

            await SendAsync(
                toEmail, recipientName,
                $"MESNET — Devamsızlık Bildirimi ({stepLabel})",
                htmlBody, ct);

            _logger.LogInformation(
                "Devamsızlık bildirimi gönderildi: {Email} — {Student}, {Step} ({Leg}, {Days} gün)",
                toEmail, studentName, stepLabel, legLabel, days);

            return Result.Success();
        }
        catch (Exception ex)
        {
            // Tebligat gönderimi başarısızsa SESSİZ KALINMAZ: md. 36 (4) yükümlülüğü yerine
            // getirilememiştir ve bunun izi olmalıdır.
            _logger.LogError(ex,
                "Devamsızlık bildirimi GÖNDERİLEMEDİ: {Email} — {Student}, {Step}",
                toEmail, studentName, stepLabel);

            return Result.Failure(new Error(
                "Attendance.NotificationEmailFailed", $"Bildirim e-postası gönderilemedi: {ex.Message}"));
        }
    }

    /// <summary>SMTP bağlantısı ve gönderim — davet ve bildirim yolları arasında ortak.</summary>
    private async Task SendAsync(
        string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var smtpHost = _configuration["SmtpSettings:Host"] ?? "localhost";
        var smtpPort = int.TryParse(_configuration["SmtpSettings:Port"], out var port) ? port : 1025;
        var smtpUser = _configuration["SmtpSettings:Username"];
        var smtpPass = _configuration["SmtpSettings:Password"];
        var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? "noreply@mesnet.local";
        var fromName = _configuration["SmtpSettings:FromName"] ?? "MESNET Sistemi";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder();

        var logoBytes = _templateService.GetLogoBytes();
        if (logoBytes.Length > 0)
        {
            var logoAttachment = builder.LinkedResources.Add("logo.png", logoBytes, new ContentType("image", "png"));
            logoAttachment.ContentId = "mesnet-logo";
            logoAttachment.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
        }

        builder.HtmlBody = htmlBody;
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        // Dev ortamda SSL yok (Mailpit), prod'da TLS var
        var useSsl = !string.IsNullOrEmpty(smtpUser);
        await client.ConnectAsync(
            smtpHost, smtpPort,
            useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            ct);

        if (!string.IsNullOrEmpty(smtpUser))
            await client.AuthenticateAsync(smtpUser, smtpPass ?? string.Empty, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
