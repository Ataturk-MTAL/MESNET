using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MESNET.Common.Shared;

/// <summary>
/// Eğitim (öğretim) yılı metninin tek kanonik biçimi: <c>"2025-2026"</c>.
/// Geçmişte aynı değer dört ayrı noktada farklı biçimlerde üretiliyordu
/// ("2025 / 2026", "2025 - 2026", yalnız "2025"). Üretim ve yazma noktaları
/// bu yardımcıdan geçer; okuma tarafında <see cref="Normalize"/> DB migration'ı
/// olmadan eski kayıtları da tek biçimde gösterir (#112).
/// </summary>
public static partial class AcademicYear
{
    /// <summary>Kanonik ayraç — tire, boşluksuz.</summary>
    public const string Separator = "-";

    /// <summary>
    /// Başlangıç ve bitiş yılından kanonik metin üretir. Örn. (2025, 2026) → "2025-2026".
    /// </summary>
    public static string Format(int startYear, int endYear) => $"{startYear}{Separator}{endYear}";

    /// <summary>
    /// Serbest biçimli eğitim yılı metnini kanonik biçime çevirir.
    /// "2025 / 2026", "2025 - 2026", "2025–2026" → "2025-2026".
    /// Tanınmayan metin olduğu gibi (yalnız baş/son boşlukları kırpılarak) döner;
    /// null veya boş girdi olduğu gibi geri verilir.
    /// </summary>
    [return: NotNullIfNotNull(nameof(raw))]
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        var trimmed = raw.Trim();
        return YearRangePattern().Replace(trimmed, $"$1{Separator}$2");
    }

    // Ayraç olarak tire (-), bölü (/) veya en tire (–) kabul edilir; çevresinde boşluk olabilir.
    // Anchor yok: değer daha uzun bir metnin içinde geçse de ("Öğretim Yılı: 2025 / 2026")
    // yalnız yıl aralığı kısmı kanonikleşir, gerisi korunur.
    [GeneratedRegex(@"(\d{4})\s*[-/–]\s*(\d{4})")]
    private static partial Regex YearRangePattern();
}
