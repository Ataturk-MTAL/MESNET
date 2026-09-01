namespace MESNET.Common.Shared.Security;

/// <summary>
/// Saklanmış aktif bağlamın bu istekte kullanılabilir olup olmadığına karar verir (B parçası).
/// </summary>
/// <remarks>
/// <para><b>İki koşul birlikte aranır:</b> bağlam bu oturumda kurulmuş olmalı (<c>sid</c>
/// eşleşmesi) ve hedef hâlâ aktörün alt ağacında olmalı.</para>
///
/// <para><b>Alt ağaç kontrolü neden HER çözümlemede tekrarlanır:</b> ağaç değişebilir — okul
/// başka ilçeye taşınabilir, kullanıcının kendi kurumu değişebilir. Yalnız bağlam kurulurken
/// doğrulansaydı, sonradan alt ağaçtan çıkan bir okula erişim sessizce sürerdi.</para>
///
/// <para><b>Geçersizlik hata değildir.</b> <c>null</c> dönülür ve çağıran ev kurumuna düşer;
/// bayat bağlam bir yetki ihlali değil, bir zamanaşımıdır.</para>
///
/// <para><b><c>sid</c> yetki kararında kullanılmaz</b>, yalnız bağlamı düşürmek için. En kötü
/// hâlde yanlış karşılaştırır ve kullanıcı okulu yeniden seçer; kapsam sızdırmaz.</para>
///
/// <para><b>Platform muafiyeti YALNIZ alt ağaç koşulunu atlar, oturum koşulunu DEĞİL</b>
/// (canlıda ölçülen sessiz hata — bkz. <c>hasPlatformScope</c>). <c>sid</c> kontrolü muafiyet
/// dahil HER aktör için geçerli kalır: platform aktörünün bağlamı da oturumlar arası
/// taşınmamalıdır, "kurum sınırının üstünde çalışma" yetkisi "oturum sınırının üstünde
/// çalışma" yetkisi değildir. Sıra bu yüzden <c>Decide</c>/<c>CanAssign</c>'daki "muafiyet en
/// önde" idiomunun harfiyen kopyası DEĞİLDİR — burada muafiyet en SON adımı (ağaç kontrolünü)
/// değiştirir, ilk iki adımı (hedef var mı, oturum güncel mi) atlamaz.</para>
/// </remarks>
public static class ActiveContextPolicy
{
    /// <param name="activeInstitutionId">Kayıttaki aktif bağlam; <c>null</c> = bağlam yok.</param>
    /// <param name="storedSessionId">Bağlamı kuran token'ın <c>sid</c>'i.</param>
    /// <param name="currentSessionId">Bu isteğin token'ındaki <c>sid</c>.</param>
    /// <param name="actorPath">Aktörün kurum ağacındaki yolu (<c>institution_path</c>).</param>
    /// <param name="targetPath">Hedef kurumun yolu.</param>
    /// <param name="hasPlatformScope">
    /// <c>platform:tenant:manage</c> — kurum sınırının üstünde çalışma yetkisi (ADR-0003 adım
    /// 6). <b>Bu parametre olmadan platform aktörü hedefi ASLA çözemezdi</b> — kendi kurumu/
    /// yolu yoktur (<c>InstitutionScopePolicy.CanAccessByPath</c> yolsuz aktörü her zaman
    /// reddeder), yani <c>SetActiveInstitutionHandler</c> geçişi kabul etse bile bu adım
    /// bağlamı her çözümlemede sessizce düşürürdü: kayıt <c>activeInstitutionId</c> taşırdı,
    /// claim hiç doğmazdı, kullanıcı hatasız biçimde ev kurumuna geri düşerdi. Ölçüldü
    /// (canlı): <c>admin</c> Gazi MTAL'a "geçti", kayıt güncellendi, ama <c>/auth/me</c>
    /// <c>active_institution_id: None</c> döndü — iki kontrol noktası (değiştirme anı ve her
    /// çözümleme) ayrışmıştı.
    /// </param>
    /// <returns>Kullanılabilir bağlamın kurum kimliği; kullanılamıyorsa <c>null</c>.</returns>
    public static Guid? Resolve(
        Guid? activeInstitutionId,
        string? storedSessionId,
        string? currentSessionId,
        string? actorPath,
        string? targetPath,
        bool hasPlatformScope)
    {
        if (activeInstitutionId is not { } target || target == Guid.Empty)
            return null;

        // Ordinal ve büyük/küçük harfe DUYARLI: sid rastgele üretilmiş bir dizedir, harf
        // katlaması iki ayrı oturumu eşitleyebilirdi. Muafiyet BUNU ATLAMAZ — bkz. sınıf
        // yorumu ve hasPlatformScope parametre açıklaması.
        if (string.IsNullOrEmpty(storedSessionId)
            || string.IsNullOrEmpty(currentSessionId)
            || !string.Equals(storedSessionId, currentSessionId, StringComparison.Ordinal))
        {
            return null;
        }

        // Muafiyet yalnız BURADAN sonrasını, yani ağaç kontrolünü, değiştirir.
        if (hasPlatformScope)
            return target;

        return InstitutionScopePolicy.CanAccessByPath(actorPath, targetPath) ? target : null;
    }
}
