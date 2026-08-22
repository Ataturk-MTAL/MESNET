using Ardalis.SmartEnum;

namespace MESNET.Attendance.Core.Enums;

/// <summary>
/// Sağlık raporunun onay durumu (#172).
///
/// <para>Devamsızlık kaydının kendi <see cref="AttendanceStatus"/>'ünden AYRIDIR: rapor eklemek
/// kaydın onay durumunu değiştirmez, kendi zincirini yürütür. Devamsızlık türünün
/// <c>HealthReport</c>'a dönmesi — yani ücret kesintisinin kalkması — yalnız
/// <see cref="Approved"/> durumunda gerçekleşir.</para>
///
/// <para>Bundan önce rapor eklendiği anda tür değişiyordu; onay adımı hiç yoktu ve giriş
/// doğrudan para sonucu doğuruyordu.</para>
/// </summary>
public sealed class HealthReportStatus : SmartEnum<HealthReportStatus>
{
    /// <summary>Kayda rapor eklenmemiş.</summary>
    public static readonly HealthReportStatus None = new(nameof(None), 0, "Yok");

    /// <summary>Rapor eklendi, koordinatör öğretmen onayı bekliyor — hüküm doğurmaz.</summary>
    public static readonly HealthReportStatus Pending = new(nameof(Pending), 1, "Onay Bekliyor");

    /// <summary>Koordinatör öğretmen onayladı — devamsızlık türü <c>HealthReport</c> oldu.</summary>
    public static readonly HealthReportStatus Approved = new(nameof(Approved), 2, "Onaylandı");

    /// <summary>Reddedildi — devamsızlık türü değişmez, kesinti uygulanmaya devam eder.</summary>
    public static readonly HealthReportStatus Rejected = new(nameof(Rejected), 3, "Reddedildi");

    public string Slug { get; }

    private HealthReportStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>Bu durumdaki bir rapor onaylanabilir/reddedilebilir mi.</summary>
    public bool CanReview => this == Pending;
}
