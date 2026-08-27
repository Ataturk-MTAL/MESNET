namespace MESNET.Common.Shared.Security;

/// <summary>Kapsam kararının sonucu.</summary>
public enum InstitutionScopeOutcome
{
    /// <summary>Erişim var; hedefin kaydını okumaya gerek yok.</summary>
    Allowed,

    /// <summary>Erişim yok; hedefin kaydını okumaya gerek yok.</summary>
    Denied,

    /// <summary>
    /// Karar ağaca bakmadan verilemez. Çağıran hedefin <c>Path</c> değerini okur ve
    /// <see cref="InstitutionScopePolicy.CanAccessByPath"/> ile bitirir.
    /// </summary>
    NeedsPathCheck
}

/// <summary>
/// Bir listeleme sorgusunun nasıl daraltılacağı.
/// </summary>
/// <param name="Unrestricted">Daraltma yok — yalnız kurum üstü aktör.</param>
/// <param name="PathPrefix">Verilmişse <c>Path.StartsWith(prefix)</c> ile daraltılır.</param>
/// <param name="InstitutionId">
/// Verilmişse <c>Id == value</c> ile daraltılır. <see cref="Guid.Empty"/> hiçbir kurumla
/// eşleşmez, yani liste boş gelir — <b>her şeyi görmek</b> yerine hiçbir şey görmek.
/// </param>
public sealed record InstitutionVisibility(bool Unrestricted, string? PathPrefix, Guid? InstitutionId);

/// <summary>
/// Bir aktörün <b>hangi kurumun</b> verisine dokunabileceğine karar verir (ADR-0003 adım 6 +
/// kurum hiyerarşisi).
///
/// <para><b>Neden kiracılık yetmiyor:</b> Marten conjoined kiracılığı satırları süzer, ama
/// <c>Institution</c> belgesi <see cref="Tenancy.DocumentTenancy.Identity"/> sınıfındadır —
/// kiracının <i>kendisi</i> olduğu için damga taşımaz. Kiracılık onu korumaz; kurum kaydına
/// dokunan uçlar kimliği <b>istekten</b> alır ve karşılaştırma yapılmazsa kimse durdurmaz.</para>
///
/// <para><b>Ölçüldü (iki okullu dev ortamı):</b> bu kontrol yokken B okulunun müdürü A okulunun
/// kaydını okudu (200, <b>7 kişilik personel listesiyle</b>), adını değiştirdi (200) ve personel
/// listesine kayıt ekledi (201). Hiçbiri hata vermedi.</para>
///
/// <para><b>İzin erişimi açar, kapsamı belirlemez</b> (ADR-0001). Kapsam kararı burada ve
/// aktörün <c>institution_id</c> / <c>institution_path</c> claim'lerinden okunur — istekten
/// DEĞİL. İki claim de sunucu tarafında kullanıcı kaydından üretilir (ADR-0003 adım 2).</para>
/// </summary>
public static class InstitutionScopePolicy
{
    /// <summary>
    /// Kimlik aşaması. Yol okuması gerektirmeyen bütün durumları burada bitirir.
    /// </summary>
    /// <param name="actorInstitutionId">Aktörün kurum kapsamı — <c>institution_id</c> claim'i.</param>
    /// <param name="targetInstitutionId">İstekte geçen hedef kurum.</param>
    /// <param name="hasPlatformScope">
    /// <c>platform:tenant:manage</c> — kurum sınırının üstünde çalışma yetkisi. Okul rollerinin
    /// hiçbirinde yoktur.
    /// </param>
    public static InstitutionScopeOutcome Decide(
        Guid? actorInstitutionId, Guid targetInstitutionId, bool hasPlatformScope)
    {
        // Sıra önemli: muafiyet ÖNCE. Platform aktörünün kendi kurumu yoktur; kapsam
        // karşılaştırmasına girseydi hiçbir okula erişemezdi.
        if (hasPlatformScope)
            return InstitutionScopeOutcome.Allowed;

        if (targetInstitutionId == Guid.Empty)
            return InstitutionScopeOutcome.Denied;

        if (Normalize(actorInstitutionId) is not { } actor)
            return InstitutionScopeOutcome.Denied;

        // Kimlik eşitliği ağaçtan ÖNCE gelir. Okul kullanıcısının kendi kurumuna erişimi hiçbir
        // ek okuma yapmadan çözülür — ve geçiş ucu koşturulmamış bir kurulumda (yollar boş)
        // kurum sayfası çalışmaya devam eder.
        if (actor == targetInstitutionId)
            return InstitutionScopeOutcome.Allowed;

        return InstitutionScopeOutcome.NeedsPathCheck;
    }

    /// <summary>
    /// Ağaç aşaması. Hedef, aktörün alt ağacında mı? Aktörün kendi düğümü de alt ağacındadır;
    /// <b>üst düğüm ve kardeşler değildir</b>.
    /// </summary>
    public static bool CanAccessByPath(string? actorPath, string? targetPath) =>
        InstitutionPath.Contains(actorPath, targetPath);

    /// <summary>
    /// Bir listeleme sorgusunun nasıl daraltılacağı.
    ///
    /// <para><b>Yolu olmayan aktör kendi kurumuna daralır</b>, boşa değil. Spec harfiyen
    /// uygulansaydı (yolu boş aktör hiçbir şey görür), geçiş ucu koşturulmadan yapılan bir
    /// dağıtım her okul müdürünün kurum sayfasını kırardı. Bu daraltma bir genişletme değildir:
    /// yolu olmayan aktör hiçbir şey kazanmaz, yalnız bugünkü hakkını kaybetmez.</para>
    /// </summary>
    public static InstitutionVisibility VisibleScope(
        Guid? actorInstitutionId, string? actorPath, bool hasPlatformScope)
    {
        if (hasPlatformScope)
            return new InstitutionVisibility(Unrestricted: true, PathPrefix: null, InstitutionId: null);

        if (InstitutionPath.Normalize(actorPath) is { } path)
            return new InstitutionVisibility(Unrestricted: false, PathPrefix: path, InstitutionId: null);

        // Yol yok: kimliğe düş. Kapsamsız aktörde bu Guid.Empty'dir ve hiçbir kurumla eşleşmez.
        return new InstitutionVisibility(
            Unrestricted: false,
            PathPrefix: null,
            InstitutionId: Normalize(actorInstitutionId) ?? Guid.Empty);
    }

    /// <summary>Boş Guid ile <c>null</c> aynı anlama gelir: kapsam yok.</summary>
    private static Guid? Normalize(Guid? value) =>
        value is { } id && id != Guid.Empty ? id : null;
}
