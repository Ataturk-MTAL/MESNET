using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

/// <summary>
/// Toplu saat dağıtımında kırılan kısıtın türü (#117).
///
/// <para><see cref="SmartEnum{T}.Name"/> değerleri bilinçli olarak
/// <c>CoordinationErrors</c> içindeki hata kodlarının son ekiyle birebir aynıdır
/// (<c>Coordination.{Name}</c>) — tekil uç noktanın ürettiği kodlarla toplu ucun
/// kodları ayrışmasın, frontend tek bir eşleme tablosu tutsun.</para>
/// </summary>
public sealed class HoursViolationKind : SmartEnum<HoursViolationKind>
{
    /// <summary>Ücretli satırda saat 0 veya negatif.</summary>
    public static readonly HoursViolationKind InvalidAssignedHours =
        new(nameof(InvalidAssignedHours), 1, "geçerli saat aralığı");

    /// <summary>Satır kendi mesafe tavanını (<c>max_i</c>) aşıyor.</summary>
    public static readonly HoursViolationKind AssignedHoursExceedMax =
        new(nameof(AssignedHoursExceedMax), 2, "işletme saat tavanı");

    /// <summary>Alanın toplamı ders yükü havuzunu (<c>P</c>) aşıyor.</summary>
    public static readonly HoursViolationKind WorkloadPoolExceeded =
        new(nameof(WorkloadPoolExceeded), 3, "ders yükü havuzu");

    /// <summary>Bir öğretmenin toplamı azami haftalık ek ders saatini aşıyor.</summary>
    public static readonly HoursViolationKind TeacherHoursExceedMax =
        new(nameof(TeacherHoursExceedMax), 4, "öğretmen azami ek ders saati");

    private HoursViolationKind(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>Kısıtın Türkçe adı — hata mesajında "hangi kısıt" bilgisini taşır.</summary>
    public string Slug { get; }
}
