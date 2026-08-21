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
            .Replace("{{FullName}}", HtmlEscape(fullName))
            .Replace("{{TargetRole}}", HtmlEscape(targetRole))
            // Bağlantı sistemin kendi ürettiği URL'dir ama yine kaçışlanır: tek satırlık bir
            // istisna bırakmak, bir sonraki geliştiriciye "burada kaçışlamak isteğe bağlı"
            // mesajı verirdi.
            .Replace("{{RegistrationLink}}", HtmlEscape(registrationLink));
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
            .Replace("{{RecipientName}}", HtmlEscape(recipientName))
            .Replace("{{StudentName}}", HtmlEscape(studentName))
            .Replace("{{StepLabel}}", HtmlEscape(stepLabel))
            .Replace("{{LegLabel}}", HtmlEscape(legLabel))
            .Replace("{{Days}}", days.ToString())
            // skippedNotice sistemin ürettiği metindir (sayılar + sabit ifade), ama aynı
            // gerekçeyle o da kaçışlanır.
            .Replace("{{SkippedNotice}}", HtmlEscape(skippedNotice));
    }

    /// <summary>
    /// Yer tutucuya konan değeri HTML bağlamı için güvenli hâle getirir.
    ///
    /// <para><b>Neden gerekli:</b> şablon MJML'den HTML'e derlendikten SONRA yer tutucular düz
    /// metin değiştirmeyle doluyor. Değerlerin bir kısmı <b>kullanıcı kontrollüdür</b> —
    /// <c>RecipientName</c> ve <c>StudentName</c> <c>UserAccount.FullName</c>'den, yani
    /// kayıt sırasında girilen ad-soyaddan geliyor. Kaçışlanmazsa adına
    /// <c>&lt;img src=x onerror=...&gt;</c> yazan bir kullanıcı, velinin ve işletmenin aldığı
    /// e-postanın gövdesine <b>kendi işaretlemesini</b> sokar: düzen bozulur, sahte bağlantı ya
    /// da izleme pikseli gömülebilir. Alıcı, iletinin okuldan geldiğini varsayar — bu yüzden
    /// oltalama için elverişli bir yüzey.</para>
    ///
    /// <para><b>Neden <c>WebUtility.HtmlEncode</c> değil:</b> o, ASCII dışı karakterleri de
    /// sayısal varlığa çevirir (<c>ç</c> → <c>&amp;#231;</c>). Doğru render eder ama ham gövde
    /// okunmaz hâle gelir ve bu depoda Türkçe karakterler bilinçli olarak korunuyor. Burada
    /// yalnız HTML'de anlam taşıyan beş karakter kaçışlanır; Türkçe harfler olduğu gibi kalır.
    /// <c>&amp;</c> İLK sırada olmak zorunda — sonra gelseydi kendi ürettiği varlıkları yeniden
    /// kaçışlardı.</para>
    /// </summary>
    private static string HtmlEscape(string? value) => value is null
        ? string.Empty
        : value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);

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
