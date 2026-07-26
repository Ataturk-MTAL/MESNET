using System.Globalization;

namespace MESNET.Coordination.Core.Services;

/// <summary>Koordinatörün elle kilitlediği tek satır: işletme + korunacak saat.</summary>
/// <param name="Hours">
/// Kilitli saat. <c>0</c> = fahri ziyaret olarak kilitlendi (algoritma 0 saatlik satırı
/// fahri kovasına koyar), <c>&gt; 0</c> = ücretli saat aynen korunur.
/// </param>
public sealed record PinnedHours(Guid BusinessId, int Hours);

/// <summary>
/// Sabitlenmiş (kilitli) satırların sorgu dizesi gösterimini çözer (issue #116/#118).
///
/// <para>Biçim: <c>{işletmeKimliği}:{saat}</c> çiftleri virgülle ayrılır —
/// örnek <c>"3f2a...:6,9b1c...:0"</c>. Öneri bir <b>GET</b> sorgusu olduğu için kilitli
/// satırlar gövdeyle değil sorgu dizesiyle taşınır; tek bir parametre olması axios'un
/// dizi serileştirme biçimine bağımlılığı da ortadan kaldırır.</para>
///
/// <para>Saf ve toleranssızdır: bozuk bir çift <b>sessizce atlanmaz</b>, hata olarak
/// bildirilir — yanlış çözülen bir kilit koordinatörün elle girdiği saati yok eder.</para>
/// </summary>
public static class PinnedHoursSelection
{
    /// <summary>Çiftleri ayıran karakter.</summary>
    public const char PairSeparator = ',';

    /// <summary>Bir çift içinde kimlik ile saati ayıran karakter.</summary>
    public const char FieldSeparator = ':';

    private static readonly IReadOnlyList<PinnedHours> Empty = [];

    /// <summary>
    /// Sorgu dizesini kilitli satır listesine çevirir.
    /// </summary>
    /// <param name="raw">Ham değer. Null/boş → boş liste, hata yok (kilit yoktur).</param>
    /// <param name="pinned">Çözülen satırlar; hata durumunda boş liste.</param>
    /// <param name="error">
    /// Başarısızlık nedeni (Türkçe, kullanıcıya gösterilebilir); başarıda <c>null</c>.
    /// </param>
    /// <returns>Tümü çözülebildiyse <c>true</c>.</returns>
    public static bool TryParse(
        string? raw,
        out IReadOnlyList<PinnedHours> pinned,
        out string? error)
    {
        pinned = Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(raw)) return true;

        var parsed = new List<PinnedHours>();
        var seen = new HashSet<Guid>();

        foreach (var token in raw.Split(PairSeparator, StringSplitOptions.RemoveEmptyEntries |
                                                       StringSplitOptions.TrimEntries))
        {
            if (!TryParsePair(token, out var pair, out error)) return false;

            if (!seen.Add(pair!.BusinessId))
            {
                error = $"Aynı işletme birden çok kez kilitlenemez: {pair.BusinessId}";
                return false;
            }

            parsed.Add(pair);
        }

        pinned = parsed;
        return true;
    }

    private static bool TryParsePair(string token, out PinnedHours? pair, out string? error)
    {
        pair = null;
        error = null;

        var fields = token.Split(FieldSeparator);
        if (fields.Length != 2)
        {
            error = $"Kilitli satır biçimi geçersiz: «{token}». Beklenen biçim: işletmeKimliği:saat";
            return false;
        }

        if (!Guid.TryParse(fields[0], out var businessId) || businessId == Guid.Empty)
        {
            error = $"Kilitli satırdaki işletme kimliği geçersiz: «{fields[0]}»";
            return false;
        }

        if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours) ||
            hours < 0)
        {
            error = $"Kilitli satırdaki saat geçersiz: «{fields[1]}». Negatif olmayan bir tam sayı olmalıdır.";
            return false;
        }

        pair = new PinnedHours(businessId, hours);
        return true;
    }
}
