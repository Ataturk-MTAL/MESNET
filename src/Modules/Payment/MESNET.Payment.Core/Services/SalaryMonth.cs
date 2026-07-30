namespace MESNET.Payment.Core.Services;

/// <summary>
/// Maaş ayı (<c>yyyy-MM</c>) ile tarih arasındaki dönüşümler.
/// </summary>
public static class SalaryMonth
{
    /// <summary>
    /// Hesaplanan ayda yürürlükte olan <c>SalaryCalculationConfig</c>'i seçmek için kullanılacak
    /// tarih: ayın SON günü, saat 00:00 UTC.
    /// </summary>
    /// <remarks>
    /// Neden hesabın çalıştığı an (<c>DateTime.UtcNow</c>) değil: asgari ücret yıl içinde birden
    /// fazla kez artabilir. Aralık 2026 dönemleri Ocak 2027'de açılırsa "şu an" ile seçim
    /// Aralık maaşına 2027 asgari ücretini uygular. Dahası devamsızlıkla tetiklenen yeniden
    /// hesap ay içindeki devamsızlık tarihini kullanıyor; iki yol aynı dönem için iki farklı
    /// tutar üretiyordu. Aydan türetince hesap ne zaman koşarsa koşsun sonuç aynı olur.
    ///
    /// Neden ayın sonu, günün başı: yürürlük zinciri eski kaydı <c>EffectiveFrom.AddDays(-1)</c>
    /// ile kapatır, yani <c>EffectiveTo</c> o günün başındadır. Gün sonu (23:59) seçilirse
    /// <c>EffectiveTo &gt;= referenceDate</c> koşulu kendi kapanış gününde patlar ve hiç config
    /// bulunmaz (<c>SalaryConfigMissing</c> → HTTP 422).
    ///
    /// Ay ORTASINDA yürürlüğe giren bir artış ayın tamamına uygulanır; tutar bölüşülmez —
    /// kısmi ay hesabı yok (bkz. #154, 3308 bu konuda sessiz).
    /// </remarks>
    /// <param name="month">Ay, <c>yyyy-MM</c> formatında (ör. <c>2026-12</c>).</param>
    /// <param name="fallback">Ay çözümlenemezse kullanılacak tarih.</param>
    public static DateTime ConfigReferenceDate(string? month, DateTime fallback)
    {
        if (!TryParse(month, out var year, out var monthNumber))
            return fallback;

        return new DateTime(
            year, monthNumber, DateTime.DaysInMonth(year, monthNumber),
            0, 0, 0, DateTimeKind.Utc);
    }

    private static bool TryParse(string? month, out int year, out int monthNumber)
    {
        year = 0;
        monthNumber = 0;

        if (string.IsNullOrWhiteSpace(month)) return false;

        var parts = month.Split('-');
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0], out year)
               && int.TryParse(parts[1], out monthNumber)
               && year is >= 1 and <= 9999
               && monthNumber is >= 1 and <= 12;
    }
}
