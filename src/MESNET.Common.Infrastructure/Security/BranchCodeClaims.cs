using System.Security.Claims;
using System.Text.Json;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// <c>branch_codes</c> claim'inin okunması ve adının tek yerde tutulması (#126).
///
/// <para>Claim üç ayrı biçimde gelebilir ve üçü de desteklenir:</para>
/// <list type="bullet">
///   <item>Keycloak <c>multivalued</c> öznitelik mapper'ı → aynı tipte birden çok claim</item>
///   <item>JSON dizi metni (<c>["EET","MTT"]</c>) — bazı mapper yapılandırmaları böyle üretir</item>
///   <item>Virgülle ayrılmış tek metin (<c>"EET,MTT"</c>) — elle girilmiş öznitelik</item>
/// </list>
/// </summary>
public static class BranchCodeClaims
{
    /// <summary>Token/claim adı — Keycloak protocol mapper'ı ile aynı olmalıdır.</summary>
    public const string ClaimType = "branch_codes";

    /// <summary>
    /// Kullanıcının alan kodlarını okur. Bulunamazsa boş liste döner —
    /// boş liste "kapsam bilinmiyor" demektir ve yazma isteğini reddettirir.
    /// </summary>
    public static IReadOnlyList<string> Read(ClaimsPrincipal principal)
    {
        var codes = new List<string>();

        foreach (var claim in principal.FindAll(ClaimType))
            codes.AddRange(Parse(claim.Value));

        return codes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Tek bir claim değerini alan kodlarına ayrıştırır.</summary>
    public static IEnumerable<string> Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return [];

        var trimmed = rawValue.Trim();

        if (trimmed.StartsWith('[') && TryParseJsonArray(trimmed, out var fromJson))
            return fromJson;

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool TryParseJsonArray(string value, out IReadOnlyList<string> codes)
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                codes = [];
                return false;
            }

            codes = doc.RootElement.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .ToList();
            return true;
        }
        catch (JsonException)
        {
            codes = [];
            return false;
        }
    }
}
