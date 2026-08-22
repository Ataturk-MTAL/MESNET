namespace MESNET.Common.Shared.Security;

/// <summary>
/// Bir aktörün <b>hangi okulun</b> verisine dokunabileceğine karar verir (ADR-0003 adım 6).
///
/// <para><b>Neden kiracılık yetmiyor:</b> Marten conjoined kiracılığı satırları süzer, ama
/// <c>Institution</c> belgesi <see cref="Tenancy.DocumentTenancy.Identity"/> sınıfındadır —
/// kiracının <i>kendisi</i> olduğu için damga taşımaz. Kiracılık onu korumaz; kurum kaydına
/// dokunan uçlar kimliği <b>istekten</b> alır ve karşılaştırma yapılmazsa kimse durdurmaz.</para>
///
/// <para><b>Ölçüldü (iki okullu dev ortamı, ADR-0003 adım 6 denemesi):</b> bu kontrol yokken
/// B okulunun müdürü A okulunun kaydını okudu (200, <b>7 kişilik personel listesiyle</b>),
/// adını değiştirdi (200) ve personel listesine kayıt ekledi (201). Hiçbiri hata vermedi.</para>
///
/// <para><b>İzin erişimi açar, kapsamı belirlemez</b> (ADR-0001). <c>institution:manage</c>
/// "kurum yönetebilir" der, "hangi kurumu" demez. Kapsam kararı burada ve aktörün
/// <c>institution_id</c> claim'inden okunur — istekten DEĞİL.</para>
/// </summary>
public static class InstitutionScopePolicy
{
    /// <param name="actorInstitutionId">
    /// Aktörün kurum kapsamı — <c>institution_id</c> claim'inden. Claim sunucu tarafında
    /// üretilir (ADR-0003 adım 2), yani kullanıcı belirleyemez.
    /// </param>
    /// <param name="targetInstitutionId">İstekte geçen hedef kurum.</param>
    /// <param name="hasPlatformScope">
    /// <c>platform:tenant:manage</c> — kurum sınırının üstünde çalışma yetkisi. Yeni okul açan
    /// ve ilk kullanıcısını bağlayan aktörün bunu taşıması gerekir; okul rollerinin hiçbirinde
    /// yoktur.
    /// </param>
    public static bool CanAccess(
        Guid? actorInstitutionId, Guid targetInstitutionId, bool hasPlatformScope)
    {
        // Sıra önemli: muafiyet ÖNCE. Platform aktörünün kendi okulu yoktur; kapsam
        // karşılaştırmasına girseydi hiçbir okula erişemezdi.
        if (hasPlatformScope)
            return true;

        if (targetInstitutionId == Guid.Empty)
            return false;

        return Normalize(actorInstitutionId) is { } actor && actor == targetInstitutionId;
    }

    /// <summary>
    /// Bir listeleme sorgusunun hangi kuruma daraltılacağı. <c>null</c> = daraltma yok
    /// (yalnız platform aktörü). Kapsamsız aktör için <see cref="Guid.Empty"/> döner: hiçbir
    /// kurumla eşleşmez, yani liste boş gelir — <b>her şeyi görmek</b> yerine hiçbir şey görmek.
    /// </summary>
    public static Guid? VisibleInstitutionFilter(Guid? actorInstitutionId, bool hasPlatformScope) =>
        hasPlatformScope ? null : Normalize(actorInstitutionId) ?? Guid.Empty;

    /// <summary>Boş Guid ile <c>null</c> aynı anlama gelir: kapsam yok.</summary>
    private static Guid? Normalize(Guid? value) =>
        value is { } id && id != Guid.Empty ? id : null;
}
