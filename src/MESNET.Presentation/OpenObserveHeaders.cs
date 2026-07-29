using System.Text;

namespace MESNET.Presentation;

/// <summary>
/// OpenObserve OTLP alıcısının beklediği kimlik ve yönlendirme başlıkları.
///
/// <para>OpenObserve, OTLP gövdesinin nereye yazılacağını başlıklardan okur:
/// <c>Authorization</c> (Basic), <c>organization</c> (yalnız gRPC'de anlamlı) ve
/// <c>stream-name</c>. Bunlar olmadan bağlantı kurulur ama kayıt reddedilir — sessiz
/// log kaybı demektir, bu yüzden eksik yapılandırma sessizce geçilmez.</para>
/// </summary>
public static class OpenObserveHeaders
{
    private const string DefaultOrganization = "default";
    private const string DefaultStream = "mesnet";

    /// <summary>
    /// Başlıkları üretir. OpenObserve yapılandırılmamışsa <c>null</c> döner —
    /// bu bir hata değildir: Aspire dashboard'a yazan geliştirme kurulumunda
    /// kimlik başlığı gerekmez.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Uç nokta verilmiş ama kimlik bilgisi eksikse. Sessizce başlıksız göndermek,
    /// tüm logların sunucu tarafında reddedilip kaybolmasına yol açardı; hatanın
    /// başlangıçta görünür olması yeğdir.
    /// </exception>
    public static IDictionary<string, string>? Build(IConfiguration configuration)
    {
        var endpoint = configuration["OpenObserve:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint)) return null;

        var user = configuration["OpenObserve:User"];
        var password = configuration["OpenObserve:Password"];

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "OpenObserve:Endpoint verildi ama OpenObserve:User / OpenObserve:Password eksik. " +
                "Kimlik başlığı olmadan gönderilen loglar reddedilir ve sessizce kaybolur.");
        }

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));

        return new Dictionary<string, string>
        {
            ["Authorization"] = $"Basic {credentials}",
            ["organization"] = configuration["OpenObserve:Organization"] ?? DefaultOrganization,
            ["stream-name"] = configuration["OpenObserve:Stream"] ?? DefaultStream,
        };
    }
}
