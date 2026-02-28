using System.Reflection;
using Microsoft.Extensions.Logging;
using Mjml.Net;

namespace MESNET.Common.Infrastructure.Email;

public sealed class MjmlEmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<MjmlEmailTemplateService> _logger;
    private readonly Lazy<string> _invitationHtmlTemplate;
    private readonly Lazy<byte[]> _logoBytes;

    private static readonly Assembly ResourceAssembly = typeof(MjmlEmailTemplateService).Assembly;

    public MjmlEmailTemplateService(ILogger<MjmlEmailTemplateService> logger)
    {
        _logger = logger;
        _invitationHtmlTemplate = new Lazy<string>(CompileInvitationTemplate);
        _logoBytes = new Lazy<byte[]>(LoadLogoBytes);
    }

    public string RenderInvitation(string fullName, string targetRole, string registrationLink)
    {
        return _invitationHtmlTemplate.Value
            .Replace("{{FullName}}", fullName)
            .Replace("{{TargetRole}}", targetRole)
            .Replace("{{RegistrationLink}}", registrationLink);
    }

    public byte[] GetLogoBytes() => _logoBytes.Value;

    private string CompileInvitationTemplate()
    {
        var mjml = ReadEmbeddedResource("MESNET.Common.Infrastructure.Email.Templates.invitation.mjml");
        var renderer = new MjmlRenderer();

        var (html, errors) = renderer.Render(mjml, new MjmlOptions { Beautify = false });

        if (errors.Count > 0)
        {
            foreach (var error in errors)
                _logger.LogWarning("MJML template hatası: {Error}", error);
        }

        _logger.LogInformation("MJML invitation template derlendi ({Length} karakter HTML)", html.Length);
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
