using System.Text.RegularExpressions;

namespace MESNET.Common.Shared.Telemetry;

/// <summary>
/// Tarayıcıdan gelen hata kaydını sunucu loguna yazmadan önce temizler (#144).
///
/// <para><b>Neden gerekli:</b> kabul ucu <b>anonimdir</b> — kimlik doğrulaması isteseydi en çok
/// ihtiyaç duyulan anda çalışmazdı (#136'da hata tam olarak oturum kurulamadığı için yaşandı).
/// Yani gövdeyi tamamen istemci belirler ve sunucu ona güvenemez.</para>
///
/// <para><b>İki iş yapar:</b> gizleme (token, e-posta, kimlik numarası) ve kırpma (sınırsız metin
/// log hattını şişirir). İkisi de zorunludur; biri atlanırsa diğeri işe yaramaz.</para>
///
/// <para><b>Denge:</b> her şeyi gizleyen bir temizleyici kaydı çöpe çevirir. HTTP durum kodu,
/// satır numarası ve sayaç gibi <b>tanı için gerekli</b> kısa sayılar korunur — issue'nun çıkış
/// noktası olan satır zaten böyle bir metindi:
/// <c>[Auth] /auth/me henüz hazır değil (durum: 401), 1.5s sonra tekrar... (1/5)</c></para>
/// </summary>
public static partial class ClientErrorSanitizer
{
    public const string RedactionMarker = "[gizlendi]";
    public const string TruncationSuffix = "…[kırpıldı]";

    /// <summary>JWT: üç base64url parçası, nokta ayraçlı. Log'a düşen geçerli token = oturum.</summary>
    [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*", RegexOptions.Compiled)]
    private static partial Regex JwtPattern();

    /// <summary><c>Bearer &lt;değer&gt;</c> — token JWT biçiminde olmasa da başlıkla gelir.</summary>
    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    /// <summary>
    /// 11+ haneli sayı dizisi. Gerçekten T.C. kimlik numarası mı diye <b>bakılmaz</b>: yanlış
    /// gizleme zararsız, yanlış sızdırma değil.
    /// </summary>
    [GeneratedRegex(@"\b\d{11,}\b", RegexOptions.Compiled)]
    private static partial Regex LongDigitsPattern();

    /// <summary>Gizlenecek desenleri işaretle değiştirir. Kısa sayılar ve tanı metni korunur.</summary>
    public static string? Redact(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Sıra anlamlı: JWT önce gelir, yoksa Bearer deseni token'ın yalnız başını yakalar.
        var result = JwtPattern().Replace(value, RedactionMarker);
        result = BearerPattern().Replace(result, $"Bearer {RedactionMarker}");
        result = EmailPattern().Replace(result, RedactionMarker);
        result = LongDigitsPattern().Replace(result, RedactionMarker);

        return result;
    }

    /// <summary>Uzunluk sınırı — anonim uç sınırsız metin kabul etmemeli.</summary>
    public static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;

        return value[..maxLength] + TruncationSuffix;
    }

    /// <summary>Gizle + kırp. Loga yazılacak her alan bundan geçer.</summary>
    public static string? Clean(string? value, int maxLength) => Truncate(Redact(value), maxLength);
}
