using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using MESNET.Common.Infrastructure.Email;
using MESNET.Common.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MESNET.Security.Application.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly IEmailTemplateService _templateService;

    public SmtpEmailService(
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger,
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

            using var client = new SmtpClient(smtpHost, smtpPort);

            if (!string.IsNullOrEmpty(smtpUser))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;
            }

            var htmlBody = _templateService.RenderInvitation(fullName, targetRole, registrationLink);

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "MESNET — Sisteme Kayıt Davetiyesi",
            };
            message.To.Add(new MailAddress(toEmail, fullName));

            // Logo'yu CID attachment olarak ekle
            var logoBytes = _templateService.GetLogoBytes();
            if (logoBytes.Length > 0)
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
                var logoResource = new LinkedResource(new MemoryStream(logoBytes), MediaTypeNames.Image.Png)
                {
                    ContentId = "mesnet-logo",
                    TransferEncoding = TransferEncoding.Base64
                };
                htmlView.LinkedResources.Add(logoResource);
                message.AlternateViews.Add(htmlView);
            }
            else
            {
                message.IsBodyHtml = true;
                message.Body = htmlBody;
            }

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
