namespace MESNET.Common.Shared.Security;

/// <summary>
/// Kurum kapsamının kullanıcı/davet sorgusuna nasıl çevrileceği — saf karar.
///
/// <para><b>Neden ayrı bir fonksiyon:</b> <c>UserAccount</c> ve <c>UserInvitation</c>
/// <c>DocumentTenancyMap</c>'te <b>kimlik katmanındadır</b>; conjoined kiracılık onları
/// SÜZMEZ. Kapsamın tamamı sorgu handler'ına aittir ve iki ayrı handler'da yaşayacağı için
/// karar tek yere çıkarılmıştır.</para>
/// </summary>
public static class UserScopePolicy
{
    /// <param name="scope">Aktörün görünürlüğü — <c>InstitutionScopePolicy.VisibleScope</c>'tan.</param>
    /// <param name="subtreeIds">
    /// <paramref name="scope"/> bir yol öneki taşıyorsa o önekin altındaki kurum kimlikleri;
    /// aksi hâlde boş liste. Çağıran bunu <c>IInstitutionSubtreeDirectory</c>'den alır.
    /// </param>
    /// <returns>
    /// <c>null</c> = süzgeç UYGULANMAZ (platform kapsamı).
    /// Boş liste = yalnız kurum bağı OLMAYAN kayıtlar görünür.
    /// Dolu liste = bu kimliklere bağlı VEYA kurum bağı olmayan kayıtlar görünür.
    ///
    /// <para><b><c>null</c> ile boş liste karıştırılırsa sonuç TERS döner</b> — biri her şeyi
    /// açar, öteki neredeyse her şeyi kapatır.</para>
    /// </returns>
    public static IReadOnlyList<Guid>? VisibleInstitutionIds(
        InstitutionVisibility scope, IReadOnlyList<Guid> subtreeIds)
    {
        // EN ÖNDE: platform aktörünün kurumu olmayabilir. Bu dal sonda olsaydı kapsamsız
        // sayılıp Guid.Empty'ye düşerdi ve HER ZAMAN boş liste görürdü — sessiz hata.
        if (scope.Unrestricted)
            return null;

        if (!string.IsNullOrWhiteSpace(scope.PathPrefix))
            return subtreeIds;

        // Yolu olmayan aktör kendi kurumuna daralır. Kapsamsız aktörde bu Guid.Empty'dir ve
        // hiçbir kurumla eşleşmez — her şeyi görmek yerine hiçbir şey görmek.
        return scope.InstitutionId is { } id && id != Guid.Empty ? [id] : [];
    }

    /// <summary>
    /// TEK bir kaydın kapsamda olup olmadığı — kimliğiyle yüklenen kayıtlar için (#284).
    ///
    /// <para><b>Neden ayrı bir kapı gerekiyor:</b> liste sorgusu <see cref="VisibleInstitutionIds"/>
    /// dönüşünü <c>Where</c>'e çevirir, ama <c>LoadAsync&lt;T&gt;(id)</c> ile TEK kayıt çeken yol
    /// hiçbir <c>Where</c>'den geçmez. Kimlikle çekmek "zaten kapsamlı" anlamına GELMEZ:
    /// tanımlayıcının tahmin edilemezliğine dayanmak yetkilendirme değildir. Ölçüldü (#284):
    /// üç davet yazma ucu tam olarak bunu yapıyordu ve başka okulun daveti onaylanabiliyordu.</para>
    ///
    /// <para><b>Kurum bağı OLMAYAN kayıt görünür kalır.</b> Okuma tarafındaki kararla aynı:
    /// aksi hâlde kapsamsız davet hiç kimse tarafından onaylanamaz/reddedilemez hâle gelir ve
    /// sonsuza kadar beklemede kalırdı.</para>
    /// </summary>
    /// <param name="visibleIds">
    /// <see cref="VisibleInstitutionIds"/> dönüşü. <c>null</c> = süzgeç yok (platform kapsamı),
    /// yani her kayıt görünür.
    /// </param>
    /// <param name="recordInstitutionId">Kaydın kurum bağı; <c>null</c> ise bağsızdır.</param>
    public static bool IsVisible(IReadOnlyList<Guid>? visibleIds, Guid? recordInstitutionId)
    {
        if (visibleIds is null)
            return true;

        if (recordInstitutionId is not { } institutionId)
            return true;

        return visibleIds.Contains(institutionId);
    }
}
