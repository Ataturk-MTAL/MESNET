namespace MESNET.Attendance.Core.Services;

/// <summary>
/// Ücretli izin onay zincirinin saf kuralları (#177). G/Ç yapmaz.
///
/// <para><b>Bu sınıf neden var:</b> iki taraflı onay <b>permission ile garanti edilemez</b>.
/// <c>InstitutionManager</c> her domain wildcard'ını taşır (<c>attendance:*</c>, <c>company:*</c>,
/// <c>department:*</c> …), yani işletme adımı için tanımlanacak izin — hangi önekte olursa olsun —
/// okul müdürüne de gider ve zincir tek tarafa çöker. <c>platform:</c> dışında serbest önek
/// yoktur. ADR-0001: <i>permission erişimi açar, KAPSAMI belirlemez</i> — kapsam kararı burada,
/// claim üzerinden verilir.</para>
/// </summary>
public static class PaidLeaveApprovalPolicy
{
    /// <summary>
    /// Tek başvurunun kapsayabileceği azami gün sayısı. Sınır iki işe yarar: yanlış girilen
    /// bitiş tarihi (ör. yıl hatası) binlerce devamsızlık kaydı açmasın, ve MEB'in "ara tatil,
    /// yarıyıl ve yaz tatili süresince toplam bir ay ücretli izin" tavanıyla aynı büyüklükte
    /// kalsın. Tavanın kendisi yıllık toplamdır; bu sınır tek başvuru içindir.
    /// </summary>
    public const int MaxLeaveDays = 60;

    /// <summary>
    /// İşletme onay adımını yapabilir mi — <b>kapsam</b> kontrolü, izin kontrolü değil.
    ///
    /// <para><paramref name="businessIdClaim"/> token'ın <c>business_id</c> claim'inden gelir ve
    /// istekten ALINMAZ. Okul rollerinde bu claim yoktur; wildcard izinle uca erişse bile adımı
    /// yapamaz. Aynı desen <c>StudentTermGradeEndpoints</c>'te kullanılıyor.</para>
    /// </summary>
    public static bool CanBusinessApprove(Guid? businessIdClaim, Guid requestBusinessId) =>
        businessIdClaim is { } claim
        && claim != Guid.Empty
        && requestBusinessId != Guid.Empty
        && claim == requestBusinessId;

    /// <summary>
    /// Okul onayını yapan, işletme onayını yapandan FARKLI bir kullanıcı mı.
    ///
    /// <para>Bir kullanıcı iki rolü birden taşıyabilir (izinler rollerin birleşimidir). O durumda
    /// tek kişi zincirin iki adımını da yürütür ve "iki taraflı onay" adı kalır, kendisi kalmaz.
    /// Bilinmeyen kimlik (<c>Guid.Empty</c>) de reddedilir: iki tarafın da kimliksiz olduğu bir
    /// onay, eşitlik kontrolünü sessizce geçerdi.</para>
    /// </summary>
    public static bool AreApproversDistinct(Guid businessApproverId, Guid schoolApproverId) =>
        businessApproverId != Guid.Empty
        && schoolApproverId != Guid.Empty
        && businessApproverId != schoolApproverId;

    /// <summary>Tarih aralığı geçerli mi — başlangıç bitişten sonra olamaz, aralık sınırı aşamaz.</summary>
    public static bool IsRangeValid(DateTime startDate, DateTime endDate) =>
        startDate.Date <= endDate.Date && DayCount(startDate, endDate) <= MaxLeaveDays;

    /// <summary>Aralıktaki toplam gün sayısı (iki uç dâhil).</summary>
    public static int DayCount(DateTime startDate, DateTime endDate) =>
        (endDate.Date - startDate.Date).Days + 1;

    /// <summary>
    /// İzin resmîleşince devamsızlık kaydı açılacak günler.
    ///
    /// <para>Kurum takvimindeki kısıtlı günler (resmî tatil, kurum tatili) ATLANIR — o günlerde
    /// zaten devam beklenmediği için izin kaydı da anlamsızdır. Hafta sonu ayrıca elenmez:
    /// MESEM'de işletme çalışma günleri kuruma göre değişir ve mevcut devamsızlık girişi de gün
    /// adına bakmaz; hangi günün kapalı olduğu bilgisi tek yerde, kurum takvimindedir.</para>
    /// </summary>
    public static IReadOnlyList<DateTime> ExpandLeaveDays(
        DateTime startDate, DateTime endDate, IEnumerable<DateTime> restrictedDays)
    {
        var restricted = restrictedDays.Select(d => d.Date).ToHashSet();
        var days = new List<DateTime>();

        for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
        {
            if (restricted.Contains(day)) continue;
            days.Add(day);
        }

        return days;
    }

    /// <summary>
    /// İki tarih aralığı çakışıyor mu — aynı öğrenci için üst üste binen başvuruyu engeller.
    /// Çakışan başvurular onaylanırsa aynı güne iki kez izin kaydı açılırdı.
    /// </summary>
    public static bool Overlaps(
        DateTime firstStart, DateTime firstEnd, DateTime secondStart, DateTime secondEnd) =>
        firstStart.Date <= secondEnd.Date && secondStart.Date <= firstEnd.Date;
}
