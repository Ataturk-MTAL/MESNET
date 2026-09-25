using Npgsql;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Bir istisnanın row-level security ihlali olup olmadığını söyler (#318, kural 7). Saf fonksiyon.
///
/// <para><b>Neden ayrı sınıflandırılır:</b> bu hatalar normal akışta HİÇ oluşmamalıdır —
/// başka okulun damgasıyla yazma (WITH CHECK) ya da kiracı ayarı olmadan kiracılı tabloya
/// dokunma. Oluşuyorsa ya kiracısız bir kod yolu (hata) ya da bir saldırı girişimidir. Genel
/// 500'lerin arasında kaybolursa ikisi de fark edilmez.</para>
///
/// <para>Aynı SQL durum kodu RLS dışı hatalarda da kullanılır (42501 sıradan "permission
/// denied", 42704 başka bir tanımsız ayar) — mesaj da denetlenir, yoksa alarm gürültüye boğulur.</para>
/// </summary>
public static class RlsViolationClassifier
{
    private const string InsufficientPrivilege = "42501";
    private const string UndefinedObject = "42704";

    /// <returns>İhlalin kısa açıklaması; RLS ihlali değilse <c>null</c>.</returns>
    public static string? Classify(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is not PostgresException pg)
                continue;

            if (pg.SqlState == InsufficientPrivilege
                && pg.MessageText.Contains("row-level security", StringComparison.OrdinalIgnoreCase))
            {
                return "başka kiracının damgasıyla yazma (WITH CHECK)";
            }

            if (pg.SqlState == UndefinedObject
                && pg.MessageText.Contains($"\"{TenantRls.SettingName}\"", StringComparison.Ordinal))
            {
                return "kiracı ayarı olmadan kiracılı tabloya erişim";
            }
        }

        return null;
    }
}
