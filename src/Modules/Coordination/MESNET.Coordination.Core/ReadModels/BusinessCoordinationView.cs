using MESNET.Common.Shared;

namespace MESNET.Coordination.Core.ReadModels;

/// <summary>
/// İşletme-öğretmen atama ekranı için denormalize view.
/// Coordination modülü kendi schema'sında tutar.
///
/// <para><b>Kimlik:</b> satır başına <c>(BusinessId, BranchCode, AcademicPeriodId)</c> üçlüsü —
/// <see cref="Id"/> bu üçlüden <see cref="CoordinationViewId"/> ile deterministik üretilir.
/// Aynı işletmeye iki farklı alandan öğrenci yerleştiğinde <b>iki ayrı satır</b> oluşur; her alan
/// kendi öğrenci sayısını, takdir saatini ve öğretmen atamasını taşır.</para>
///
/// <para><b>Temel satır:</b> <see cref="BranchCode"/> boş olan satır işletme düzeyi ortak
/// bilgileri (ad, adres, konum, mesafe, azami saat) tutar. Listelerde/haritada gösterilmez;
/// alan satırları oluşturulurken kaynak olarak kullanılır.</para>
/// </summary>
public sealed class BusinessCoordinationView
{
    /// <summary>Deterministik satır kimliği — <see cref="CoordinationViewId.For"/> ile üretilir.</summary>
    public Guid Id { get; init; }

    /// <summary>İşletmenin kimliği (Business modülü). Birden çok satır aynı değeri paylaşabilir.</summary>
    public Guid BusinessId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? District { get; set; }
    public Location? Location { get; set; }

    /// <summary>Okula uzaklık (otomatik Haversine veya manuel giriş)</summary>
    public double? DistanceToSchoolKm { get; set; }

    /// <summary>Lokasyon yoksa true — kullanıcı manuel mesafe girdi</summary>
    public bool IsManualDistance { get; set; }

    /// <summary>Mesafe formülünden hesaplanan maksimum koordinatörlük saati</summary>
    public int MaxCoordinationHours { get; set; }

    /// <summary>Takdir edilen (atanan) saat</summary>
    public int AssignedHours { get; set; }

    public Guid? AssignedTeacherId { get; set; }
    public string? AssignedTeacherName { get; set; }

    /// <summary>Atanan gün (Monday, Tuesday, vb.)</summary>
    public string? AssignedDay { get; set; }

    /// <summary>Atanan ders saati numarası (1-based)</summary>
    public int? AssignedPeriodNumber { get; set; }

    /// <summary>Çoklu slot atamaları (gün + ders saati)</summary>
    public List<AssignedSlotInfo> AssignedSlots { get; set; } = [];

    /// <summary>Bu işletmede aktif stajyer sayısı</summary>
    public int ActiveStudentCount { get; set; }

    /// <summary>Alan kodu (EET, BYT, MKT vb. — FieldOfStudy.Code)</summary>
    public string BranchCode { get; set; } = string.Empty;

    /// <summary>Alan adı (Elektrik-Elektronik Teknolojisi vb.)</summary>
    public string BranchName { get; set; } = string.Empty;

    public Guid InstitutionId { get; init; }
    public Guid AcademicPeriodId { get; set; }

    // ── Audit Trail ──

    /// <summary>Son değişiklik tarihi</summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>Son değişikliği yapan kişi</summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>Atama değişiklik geçmişi (en yeni en üstte)</summary>
    public List<AssignmentHistoryEntry> History { get; set; } = [];

    /// <summary>
    /// İşletme kimliğini güvenle döndürür. Çok-alanlı modele geçmeden önce yazılmış
    /// kayıtlarda <see cref="BusinessId"/> alanı yoktur; o kayıtlarda <see cref="Id"/>
    /// işletme kimliğinin ta kendisidir. Metot olduğu için JSON'a serialize edilmez.
    /// </summary>
    public Guid ResolveBusinessId() => BusinessId != Guid.Empty ? BusinessId : Id;
}

/// <summary>
/// İşletmenin atandığı tek bir slot (gün + ders saati).
/// BusinessCoordinationView.AssignedSlots koleksiyonunda tutulur.
/// </summary>
public sealed record AssignedSlotInfo(string Day, int PeriodNumber);

/// <summary>
/// İşletme atama geçmişi kaydı.
/// Her atama/kaldırma/saat değişikliği bir entry olarak saklanır.
/// </summary>
public sealed record AssignmentHistoryEntry(
    DateTime Timestamp,
    string Action,           // "Assigned", "SlotAdded", "SlotRemoved", "Unassigned", "HoursUpdated"
    string PerformedBy,
    string? TeacherName,
    string? SlotDay,
    int? SlotPeriod,
    int? AssignedHours,
    string? Details);
