using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Kurum koordinasyon yapılandırmasını (mesafe-saat tablosu, azami haftalık ek ders saati)
/// doğrulayan saf politika (#134).
///
/// <para><b>Neden var:</b> yapılandırma ucu gelen değerleri hiç denetlemeden yazıyordu.
/// Boş tablo, 0 veya negatif mesafe sınırı, negatif saat, yinelenen sınır — hepsi kabul
/// ediliyordu. Yapılandırma kurum genelidir: bozuk bir tablo tüm alanların
/// <c>MaxCoordinationHours</c> tavanlarını ve #116 dağıtım önerilerini kaydırır.</para>
///
/// <para><b>Sıralama kural değildir:</b> okuma anında
/// <c>CoordinationCalculator.CalculateMaxHours</c> tabloyu zaten <c>OrderBy</c> ile sıralar.
/// Bu yüzden "artan sırada gönderilmiş olma" şartı yoktur, yalnız içerik denetlenir.</para>
///
/// <para>Dış bağımlılığı yoktur (Marten/Wolverine görmez); girdiyi değiştirmez.</para>
/// </summary>
public static class CoordinationConfigPolicy
{
    /// <summary>Verilebilecek en düşük saat (hem kural saati hem azami haftalık ek ders saati).</summary>
    public const int MinHours = 1;

    /// <summary>Verilebilecek en yüksek saat (hem kural saati hem azami haftalık ek ders saati).</summary>
    public const int MaxHours = 40;

    /// <summary>
    /// Yapılandırmayı doğrular. Kırılan ilk kuralı döndürür, her şey geçerliyse <c>null</c>.
    /// </summary>
    /// <param name="distanceHourRules">
    /// Mesafe-saat tablosu. <c>null</c> → alan güncellenmiyor, denetlenmez (kısmi güncelleme).
    /// </param>
    /// <param name="maxWeeklyExtraHours">
    /// Azami haftalık ek ders saati. <c>null</c> → alan güncellenmiyor, denetlenmez.
    /// </param>
    public static CoordinationConfigViolation? Validate(
        IReadOnlyList<DistanceHourRule>? distanceHourRules,
        int? maxWeeklyExtraHours) =>
        ValidateDistanceHourRules(distanceHourRules) ?? ValidateMaxWeeklyExtraHours(maxWeeklyExtraHours);

    /// <summary>
    /// Mesafe-saat tablosunun içerik kuralları: dolu olmalı, her satırın sınırı pozitif ve
    /// saati aralıkta olmalı, sınırlar benzersiz olmalı, tabloda catch-all kural bulunmalı.
    /// </summary>
    private static CoordinationConfigViolation? ValidateDistanceHourRules(
        IReadOnlyList<DistanceHourRule>? rules)
    {
        if (rules is null) return null;

        if (rules.Count == 0)
            return new CoordinationConfigViolation(CoordinationConfigViolationKind.EmptyDistanceHourRules);

        foreach (var rule in rules)
        {
            var ruleViolation = ValidateRule(rule);
            if (ruleViolation is not null) return ruleViolation;
        }

        return ValidateDistinctDistances(rules) ?? ValidateUnlimitedRuleExists(rules);
    }

    /// <summary>Satır bazlı kısıtlar: <c>MaxDistanceKm &gt; 0</c> ve <c>MinHours ≤ Hours ≤ MaxHours</c>.</summary>
    private static CoordinationConfigViolation? ValidateRule(DistanceHourRule rule)
    {
        if (rule.MaxDistanceKm <= 0 || double.IsNaN(rule.MaxDistanceKm))
        {
            return new CoordinationConfigViolation(
                CoordinationConfigViolationKind.InvalidDistanceHourRuleDistance,
                DistanceKm: rule.MaxDistanceKm);
        }

        if (rule.Hours < MinHours || rule.Hours > MaxHours)
        {
            return new CoordinationConfigViolation(
                CoordinationConfigViolationKind.InvalidDistanceHourRuleHours,
                DistanceKm: rule.MaxDistanceKm,
                Hours: rule.Hours);
        }

        return null;
    }

    /// <summary>
    /// Aynı sınır iki kez geçemez — hangi saatin uygulanacağı tablodaki sıraya kalırdı,
    /// oysa sıra bilinçli olarak anlamsızdır.
    /// </summary>
    private static CoordinationConfigViolation? ValidateDistinctDistances(
        IReadOnlyList<DistanceHourRule> rules)
    {
        var seen = new HashSet<double>();

        foreach (var rule in rules)
        {
            if (seen.Add(rule.MaxDistanceKm)) continue;

            return new CoordinationConfigViolation(
                CoordinationConfigViolationKind.DuplicateDistanceHourRule,
                DistanceKm: rule.MaxDistanceKm);
        }

        return null;
    }

    /// <summary>
    /// Tabloda "üzeri (sınırsız)" catch-all kuralı (<c>double.MaxValue</c>) bulunmak
    /// ZORUNLUDUR: en büyük sınırın üstünde kalan işletmeler aksi hâlde tabloya hiç
    /// girmez ve saat hesabı tanımsız fallback'e düşerdi.
    /// </summary>
    private static CoordinationConfigViolation? ValidateUnlimitedRuleExists(
        IReadOnlyList<DistanceHourRule> rules)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator — sabit sentinel değeri
        if (rules.Max(r => r.MaxDistanceKm) == double.MaxValue) return null;

        return new CoordinationConfigViolation(
            CoordinationConfigViolationKind.MissingUnlimitedDistanceHourRule);
    }

    private static CoordinationConfigViolation? ValidateMaxWeeklyExtraHours(int? maxWeeklyExtraHours)
    {
        if (maxWeeklyExtraHours is not { } hours) return null;
        if (hours >= MinHours && hours <= MaxHours) return null;

        return new CoordinationConfigViolation(
            CoordinationConfigViolationKind.InvalidMaxWeeklyExtraHours,
            Hours: hours);
    }
}
