namespace MESNET.Coordination.Core.Entities;

/// <summary>
/// Kurum koordinatörlük ayarları (mesafe-saat eşleme, azami ders saati vb.)
/// Kurum başına tek document.
/// </summary>
public sealed class CoordinationConfig
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }

    /// <summary>Mesafe-saat eşleme tablosu (mevzuat)</summary>
    public List<DistanceHourRule> DistanceHourRules { get; set; } = DefaultRules();

    /// <summary>Büyükşehir sınırları içinde mi</summary>
    public bool IsMetropolitan { get; set; } = true;

    /// <summary>Azami haftalık ek ders saati (öğretmen başına)</summary>
    public int MaxWeeklyExtraHours { get; set; } = 20;

    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    private static List<DistanceHourRule> DefaultRules() =>
    [
        new(1.0, 2),              // ≤ 1 km → 2 saat
        new(3.0, 4),              // ≤ 3 km → 4 saat
        new(5.0, 6),              // ≤ 5 km → 6 saat
        new(double.MaxValue, 8)   // > 5 km → 8 saat
    ];
}

/// <summary>
/// Mesafe → koordinatörlük saati eşleme kuralı.
/// MaxDistanceKm'ye eşit veya küçükse Hours saat verilir.
/// </summary>
public sealed record DistanceHourRule(double MaxDistanceKm, int Hours);
