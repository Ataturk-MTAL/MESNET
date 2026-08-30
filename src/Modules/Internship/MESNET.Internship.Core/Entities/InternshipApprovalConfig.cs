namespace MESNET.Internship.Core.Entities;

/// <summary>
/// Fesih onay zincirinin "tıkanmış" sayılma eşiği — <b>ulusal parametre</b>.
///
/// <para>Kurum kimliği <b>taşımaz</b>: eşik bir işletim politikasıdır ve okul başına
/// değişmez. Yazma izni <c>platform:parameter:manage</c>'dir; hiçbir okul rolünde yoktur.
/// Emsal: <c>AttendanceLimitConfig</c> (#183).</para>
///
/// <para><b>Belge yoksa varsayılan kullanılır ve belge YAZILMAZ.</b> İlk okuma bir yazma
/// tetikleseydi okuma ucunun yan etkisi olurdu ve kiracı kararı okuma yoluna sızardı.</para>
/// </summary>
public sealed class InternshipApprovalConfig
{
    /// <summary>Tekil belge kimliği — sabittir, üretilmez.</summary>
    public static readonly Guid SingletonId = Guid.Parse("8c62ac6c-a944-4eb6-b3b0-342fe7ffc3a6");

    /// <summary>Eşik girilmemişse kullanılan gün sayısı.</summary>
    public const int DefaultStuckApprovalDays = 14;

    private const int MinThresholdDays = 1;
    private const int MaxThresholdDays = 365;

    public Guid Id { get; set; } = SingletonId;

    /// <summary>Açık onay zinciri kaç günden sonra tıkanmış sayılır.</summary>
    public int StuckApprovalDays { get; set; } = DefaultStuckApprovalDays;

    public Guid UpdatedById { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Karar saf ve tek yerdedir; handler bunu çağırır, kendi koşulunu yazmaz.
    /// </summary>
    public static bool IsValidThreshold(int days) =>
        days is >= MinThresholdDays and <= MaxThresholdDays;
}
