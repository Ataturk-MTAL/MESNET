using System.Security.Claims;
using System.Text.Json;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// <c>linked_student_ids</c> claim'inin okunması ve adının tek yerde tutulması (#174).
///
/// <para>Veli–öğrenci bağının token karşılığıdır. <b>Otoriter olan claim değil kayıttır</b>
/// (<c>UserAccount.LinkedStudentIds</c>): <see cref="PermissionClaimsTransformation"/> kayıt
/// doluysa token'dan gelen değerleri siler ve yerine kaydı koyar. Gerekçe
/// <see cref="BranchCodeClaims"/> ile aynıdır — öznitelik Keycloak'ta <i>unmanaged</i>'dır ve
/// kullanıcı kendi Account konsolundan kendine öğrenci ekleyebilirdi. <b>Token'ın imzalı olması
/// içeriğin kullanıcı tarafından belirlenmediği anlamına gelmez.</b></para>
///
/// <para>Biçim toleransı <see cref="BranchCodeClaims"/> ile aynı üç hâli kapsar: çoklu claim,
/// JSON dizi metni, virgülle ayrılmış tek metin.</para>
/// </summary>
public static class LinkedStudentClaims
{
    /// <summary>Token/claim adı — Keycloak protocol mapper'ı ile aynı olmalıdır.</summary>
    public const string ClaimType = "linked_student_ids";

    /// <summary>
    /// Kullanıcının bağlı olduğu öğrenci kimlikleri. Bulunamazsa boş liste döner — boş liste
    /// "bağ kurulmamış" demektir ve hiçbir öğrenciye erişim doğurmaz.
    /// </summary>
    public static IReadOnlyList<Guid> Read(ClaimsPrincipal principal)
    {
        var ids = new List<Guid>();

        foreach (var claim in principal.FindAll(ClaimType))
            ids.AddRange(Parse(claim.Value));

        return ids.Distinct().ToList();
    }

    /// <summary>Tek bir claim değerini öğrenci kimliklerine ayrıştırır. Bozuk değerler atlanır.</summary>
    public static IEnumerable<Guid> Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return [];

        var trimmed = rawValue.Trim();

        var tokens = trimmed.StartsWith('[') && TryParseJsonArray(trimmed, out var fromJson)
            ? fromJson
            : trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Ayrıştırılamayan değer sessizce ATLANIR, hata fırlatılmaz: bozuk tek bir kayıt
        // kullanıcının tüm oturumunu düşürmemeli. Sonuç boş kalırsa erişim zaten doğmaz.
        return tokens
            .Select(t => Guid.TryParse(t, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty);
    }

    private static bool TryParseJsonArray(string value, out IReadOnlyList<string> items)
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                items = [];
                return false;
            }

            items = doc.RootElement.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .ToList();
            return true;
        }
        catch (JsonException)
        {
            items = [];
            return false;
        }
    }
}
