using System.Reflection;
using Microsoft.Extensions.Logging;
using Mjml.Net;

namespace MESNET.Common.Infrastructure.Email;

public sealed class MjmlEmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<MjmlEmailTemplateService> _logger;
    private readonly Lazy<string> _invitationHtmlTemplate;
    private readonly Lazy<string> _absenceNotificationHtmlTemplate;
    private readonly Lazy<byte[]> _logoBytes;

    private static readonly Assembly ResourceAssembly = typeof(MjmlEmailTemplateService).Assembly;

    public MjmlEmailTemplateService(ILogger<MjmlEmailTemplateService> logger)
    {
        _logger = logger;
        _invitationHtmlTemplate = new Lazy<string>(CompileInvitationTemplate);
        _absenceNotificationHtmlTemplate = new Lazy<string>(
            () => CompileTemplate("absence-notification", "devamsızlık bildirimi"));
        _logoBytes = new Lazy<byte[]>(LoadLogoBytes);
    }

    public string RenderInvitation(string fullName, string targetRole, string registrationLink)
    {
        return _invitationHtmlTemplate.Value
            .Replace("{{FullName}}", fullName)
            .Replace("{{TargetRole}}", targetRole)
            .Replace("{{RegistrationLink}}", registrationLink);
    }

    /// <summary>
    /// Kademeli devamsızlık bildirimi (#247). Atlanan kademeler varsa ileti bunu <b>açıkça</b>
    /// söyler — zamanında yapılamamış tebligat sessizce gizlenmemeli.
    /// </summary>
    public string RenderAbsenceNotification(
        string recipientName, string studentName, string stepLabel, string legLabel,
        int days, IReadOnlyList<int> skippedSteps)
    {
        // DÜZ METİN: şablon MJML'den HTML'e ÖNCE derleniyor, yer tutucular sonra doluyor.
        // Buraya mjml etiketi koymak render edilmemiş metin bırakırdı.
        var skippedNotice = skippedSteps.Count == 0
            ? string.Empty
            : "Not: "
              + string.Join(", ", skippedSteps.Select(s => $"{s}."))
              + " gün bildirimleri zamanında yapılamamıştır.";

        return _absenceNotificationHtmlTemplate.Value
            .Replace("{{RecipientName}}", recipientName)
            .Replace("{{StudentName}}", studentName)
            .Replace("{{StepLabel}}", stepLabel)
            .Replace("{{LegLabel}}", legLabel)
            .Replace("{{Days}}", days.ToString())
            .Replace("{{SkippedNotice}}", skippedNotice);
    }

    public byte[] GetLogoBytes() => _logoBytes.Value;

    private string CompileInvitationTemplate() => CompileTemplate("invitation", "davet");

    private string CompileTemplate(string name, string aciklama)
    {
        var mjml = ReadEmbeddedResource($"MESNET.Common.Infrastructure.Email.Templates.{name}.mjml");
        var renderer = new MjmlRenderer();

        var (html, errors) = renderer.Render(mjml, new MjmlOptions { Beautify = false });

        if (errors.Count > 0)
        {
            foreach (var error in errors)
                _logger.LogWarning("MJML template hatası ({Sablon}): {Error}", name, error);
        }

        _logger.LogInformation(
            "MJML {Aciklama} template derlendi ({Length} karakter HTML)", aciklama, html.Length);
        return html;
    }

    private byte[] LoadLogoBytes()
    {
        using var stream = ResourceAssembly.GetManifestResourceStream(
            "MESNET.Common.Infrastructure.Email.Assets.logo.png");

        if (stream is null)
        {
            _logger.LogWarning("Logo embedded resource bulunamadı");
            return [];
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string ReadEmbeddedResource(string resourceName)
    {
        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource bulunamadı: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
