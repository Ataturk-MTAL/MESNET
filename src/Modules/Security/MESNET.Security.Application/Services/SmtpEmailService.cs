using System.Net;
using System.Net.Mail;
using MESNET.Common.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MESNET.Security.Application.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
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

            using var client = new SmtpClient(smtpHost, smtpPort);

            if (!string.IsNullOrEmpty(smtpUser))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;
            }

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "MESNET — Sisteme Kayıt Davetiyesi",
                IsBodyHtml = true,
                Body = $"""
                    <h2>Merhaba {fullName},</h2>
                    <p>MESNET sistemine <strong>{targetRole}</strong> olarak davet edildiniz.</p>
                    <p>Kaydınızı tamamlamak için aşağıdaki bağlantıya tıklayın:</p>
                    <p><a href="{registrationLink}" style="display:inline-block;padding:12px 24px;background:#2563eb;color:white;text-decoration:none;border-radius:6px;">Kayıt Ol</a></p>
                    <p>Bu bağlantı 7 gün geçerlidir.</p>
                    <hr/>
                    <p style="color:#666;font-size:12px;">Bu e-posta MESNET sistemi tarafından otomatik gönderilmiştir.</p>
                    """
            };
            message.To.Add(new MailAddress(toEmail, fullName));

            await client.SendMailAsync(message, ct);

            _logger.LogInformation("Davet e-postası gönderildi: {Email} ({Role})", toEmail, targetRole);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Davet e-postası gönderilemedi: {Email}", toEmail);
            return Result.Failure(new Error("Security.EmailFailed", $"E-posta gönderilemedi: {ex.Message}"));
        }
    }
}
