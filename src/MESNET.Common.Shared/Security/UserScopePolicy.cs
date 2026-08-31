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
}
