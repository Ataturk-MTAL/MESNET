namespace MESNET.Enrollment.Core.Policies;

/// <summary>
/// Yerleştirme sorgusunun kapsam merdiveni (#184). Saf fonksiyon — G/Ç yapmaz, rol adı bilmez.
/// </summary>
/// <remarks>
/// <para>Sıra: <b>geniş izin → işletme claim'i → koordinatör kaydı → boş</b>. Her basamak
/// ayrı bir yetki kaynağına dayanır; hiçbiri rol adı değildir (ADR-0001).</para>
///
/// <para><b>Neden rol adı bırakıldı:</b> rol adı organizasyon şemasının bugünkü fotoğrafıdır ve
/// o şema kayar. Eski kod bunun kanıtıydı — #129'da müdür yardımcısı ayrı role çıkınca elle
/// <c>!IsInRole(DeputyDirector)</c> eklendi; #172'de <c>CompanyHR</c> eklendiğinde
/// <b>eklenmedi</b> ve işletme İK, işletme kapsamına hiç giremiyordu. Claim'e bakan kural bu
/// bakımı gerektirmez.</para>
/// </remarks>
public static class PlacementScopePolicy
{
    /// <summary>
    /// Kapsamı çözer. <c>null</c> = kullanıcının göreceği kayıt <b>yoktur</b>.
    /// </summary>
    /// <param name="hasInstitutionWideView">Kurum genelini görme izni var mı (<c>institution:view</c>).</param>
    /// <param name="institutionId">Kurum kapsamı — token claim'inden.</param>
    /// <param name="businessIdClaim">İşletme kapsamı — token claim'inden, istekten DEĞİL.</param>
    /// <param name="businessIdFilter">Kullanıcının istediği işletme filtresi (yalnız geniş izinde geçerli).</param>
    /// <param name="coordinatorTeacherId">Kullanıcının öğretmen kaydı — yoksa <c>null</c>.</param>
    public static PlacementScope? Resolve(
        bool hasInstitutionWideView,
        Guid? institutionId,
        Guid? businessIdClaim,
        Guid? businessIdFilter,
        Guid? coordinatorTeacherId)
    {
        // 1) Okul yönetimi: kurumun tamamı. Kendi filtresini uygulayabilir.
        if (hasInstitutionWideView)
            return new PlacementScope(institutionId, TeacherId: null, BusinessId: businessIdFilter);

        // 2) İşletme: kapsam claim'den okunur ve kullanıcının filtresini EZER — kendi
        //    işletmesi dışını isteyemez.
        if (businessIdClaim is { } businessId && businessId != Guid.Empty)
            return new PlacementScope(institutionId, TeacherId: null, BusinessId: businessId);

        // 3) Koordinatör: kapsam kayıttan gelir — yerleştirmedeki TeacherId ile eşleşme.
        if (coordinatorTeacherId is { } teacherId && teacherId != Guid.Empty)
            return new PlacementScope(institutionId, teacherId, businessIdFilter);

        // 4) Çözülemedi. Sessizce kurum geneline düşmek, kapsamı belirsiz kullanıcıya her şeyi
        //    göstermek olurdu.
        return null;
    }
}

/// <summary>Çözülmüş kapsam. <c>null</c> alan = o eksende filtre yok.</summary>
/// <param name="InstitutionId">Kurum kapsamı — token claim'inden.</param>
/// <param name="TeacherId">Koordinatör kapsamı — öğretmen kaydından.</param>
/// <param name="BusinessId">İşletme kapsamı — claim'den ya da kullanıcının filtresinden.</param>
public readonly record struct PlacementScope(Guid? InstitutionId, Guid? TeacherId, Guid? BusinessId);
