namespace MESNET.Reporting.Core.Utilities;

/// <summary>
/// Sayıyı Türkçe yazıya çevirir — MEB formlarında "yazı ile" hücreleri için (#99).
/// Not puanları 0–100 aralığındadır; üst sınır emniyet payıyla 999'a kadar desteklenir.
/// </summary>
public static class TurkishNumberWords
{
    public const int MinSupportedValue = 0;
    public const int MaxSupportedValue = 999;

    private static readonly string[] Ones =
        ["", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz"];

    private static readonly string[] Tens =
        ["", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan"];

    /// <summary>
    /// Dönem puanı ortalamasını yazıya çevirir. KARAR (koordinatör, #99): yalnız tam sayı
    /// kısmı yazılır, ondalık atılır — 81.67 → "Seksen bir".
    /// </summary>
    public static string FromScore(decimal score) => ToWords((int)Math.Floor(score));

    /// <summary>0–999 arası bir tam sayıyı Türkçe yazıya çevirir. Baş harf büyüktür.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Değer desteklenen aralığın dışındaysa.</exception>
    public static string ToWords(int value)
    {
        if (value is < MinSupportedValue or > MaxSupportedValue)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Yazıya çevirme yalnız {MinSupportedValue}–{MaxSupportedValue} aralığında desteklenir.");

        if (value == 0) return "Sıfır";

        var parts = new List<string>(3);

        var hundreds = value / 100;
        // Türkçede "bir yüz" denmez, doğrudan "yüz" denir.
        if (hundreds == 1) parts.Add("yüz");
        else if (hundreds > 1) parts.Add($"{Ones[hundreds]} yüz");

        var tens = value % 100 / 10;
        if (tens > 0) parts.Add(Tens[tens]);

        var ones = value % 10;
        if (ones > 0) parts.Add(Ones[ones]);

        return Capitalize(string.Join(' ', parts));
    }

    // Türkçe büyük harf kuralı: 'i' → 'İ'. ToUpperInvariant bunu 'I' yapardı.
    private static string Capitalize(string word) =>
        word[0] == 'i'
            ? string.Concat("İ", word.AsSpan(1))
            : string.Concat(char.ToUpperInvariant(word[0]).ToString(), word.AsSpan(1));
}
