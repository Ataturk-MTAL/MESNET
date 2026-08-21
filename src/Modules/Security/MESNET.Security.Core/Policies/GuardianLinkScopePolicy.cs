namespace MESNET.Security.Core.Policies;

/// <summary>
/// Veli–öğrenci bağı <b>kiracı sınırını aşamaz</b> (#271).
///
/// <para><b>Bulunan açık:</b> bağ kurulurken öğrenci kimlikleri <b>istekten</b> geliyordu ve
/// hangi okulun öğrencisi olduğu <b>hiç kontrol edilmiyordu</b>. Bir okulun yöneticisi, başka
/// okulun öğrenci kimliğini vererek kendi kullanıcısına o öğrencinin verisine erişim
/// açabilirdi: <c>ParentScopeGuard</c> yalnız <c>LinkedStudentIds</c>'e bakar, o listenin nasıl
/// dolduğunu sorgulamaz.</para>
///
/// <para>Bu, CLAUDE.md'nin <b>"permission erişimi açar, KAPSAMI belirlemez — kapsam istekten
/// ALINMAZ"</b> kuralının doğrudan ihlaliydi ve iki kapıda birden vardı: davet
/// (<c>CreateInvitation.StudentIds</c>) ve elle bağlama
/// (<c>POST /api/security/users/{id}/students</c>).</para>
///
/// <para><b>Karar saf tutuldu</b>, veri toplama çağıranda: hangi öğrencinin kiracıya ait olduğu
/// bir sorgu sonucudur, kural değil. Aynı ayrım <c>BranchScopePolicy</c> ve
/// <c>InstitutionScopePolicy</c>'de de var.</para>
/// </summary>
public static class GuardianLinkScopePolicy
{
    /// <summary>
    /// İstenen kimliklerden <b>kiracıda bulunmayanlar</b>. Boş liste "hepsi geçerli" demektir.
    /// </summary>
    /// <param name="requested">İstekten gelen öğrenci kimlikleri.</param>
    /// <param name="knownInTenant">
    /// Kiracının öğrenci kimlikleri — kiracı damgalı görünümden okunur, yani sorgunun kendisi
    /// zaten okulla sınırlıdır.
    /// </param>
    /// <remarks>
    /// <b>Kapalı tarafa düşer:</b> bilinmeyen kimlik reddedilir, "belki başka bir kaynaktadır"
    /// diye geçilmez. Kiracı görünümü boşsa (dağıtımda <c>resync-projections</c> hiç
    /// koşmadıysa) her atama reddedilir — bu bilinçlidir: kapsamsız kalmak, yanlış kapsama
    /// düşmekten iyidir (ADR-0003 adım 2 ile aynı yön).
    /// </remarks>
    public static IReadOnlyList<Guid> FindOutOfScope(
        IEnumerable<Guid> requested, IReadOnlySet<Guid> knownInTenant)
        => requested.Where(id => !knownInTenant.Contains(id)).Distinct().ToList();

    /// <summary>Tümü kiracıya ait mi.</summary>
    public static bool AllInScope(IEnumerable<Guid> requested, IReadOnlySet<Guid> knownInTenant)
        => FindOutOfScope(requested, knownInTenant).Count == 0;
}
