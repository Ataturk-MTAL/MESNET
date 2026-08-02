using Ardalis.SmartEnum;

namespace MESNET.Attendance.Core.Enums;

/// <summary>
/// Ücretli izin başvurusunun onay zincirindeki durumu (#177).
///
/// <para>Zincir iki taraflıdır ve sırası sabittir: <see cref="PendingBusiness"/> →
/// <see cref="PendingSchool"/> → <see cref="Approved"/>. İzin ancak son adımda resmileşir;
/// o ana kadar hiçbir devamsızlık kaydı açılmaz.</para>
///
/// <para>Sıranın işletmeyle başlaması bilinçlidir: öğrencinin o gün işletmede olup olmayacağına
/// önce işveren karar verir, okul o kararı onaylar. Ters sırada okul onayı işletmeyi bağlardı.</para>
/// </summary>
public sealed class PaidLeaveStatus : SmartEnum<PaidLeaveStatus>
{
    /// <summary>Öğrenci başvurdu, işletme onayı bekleniyor.</summary>
    public static readonly PaidLeaveStatus PendingBusiness =
        new(nameof(PendingBusiness), 1, "İşletme Onayı Bekliyor");

    /// <summary>İşletme onayladı, okul (müdür yardımcısı/müdür) onayı bekleniyor.</summary>
    public static readonly PaidLeaveStatus PendingSchool =
        new(nameof(PendingSchool), 2, "Okul Onayı Bekliyor");

    /// <summary>İki taraf da onayladı — izin resmîdir, devamsızlık kayıtları açılır.</summary>
    public static readonly PaidLeaveStatus Approved = new(nameof(Approved), 3, "Resmileşti");

    /// <summary>Herhangi bir adımda reddedildi — başvuru kapandı, kayıt açılmaz.</summary>
    public static readonly PaidLeaveStatus Rejected = new(nameof(Rejected), 4, "Reddedildi");

    public string Slug { get; }

    private PaidLeaveStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>İşletme adımı bu durumda yapılabilir mi.</summary>
    public bool CanBusinessApprove => this == PendingBusiness;

    /// <summary>Okul adımı bu durumda yapılabilir mi — işletme onayı ŞART.</summary>
    public bool CanSchoolApprove => this == PendingSchool;

    /// <summary>Reddedilebilir mi — kapanmış başvuru yeniden reddedilemez.</summary>
    public bool CanReject => this == PendingBusiness || this == PendingSchool;

    /// <summary>Başvuru kapandı mı (onaylandı ya da reddedildi).</summary>
    public bool IsFinal => this == Approved || this == Rejected;
}
