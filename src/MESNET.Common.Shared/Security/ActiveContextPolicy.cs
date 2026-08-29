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
/// </remarks>
public static class ActiveContextPolicy
{
    /// <param name="activeInstitutionId">Kayıttaki aktif bağlam; <c>null</c> = bağlam yok.</param>
    /// <param name="storedSessionId">Bağlamı kuran token'ın <c>sid</c>'i.</param>
    /// <param name="currentSessionId">Bu isteğin token'ındaki <c>sid</c>.</param>
    /// <param name="actorPath">Aktörün kurum ağacındaki yolu (<c>institution_path</c>).</param>
    /// <param name="targetPath">Hedef kurumun yolu.</param>
    /// <returns>Kullanılabilir bağlamın kurum kimliği; kullanılamıyorsa <c>null</c>.</returns>
    public static Guid? Resolve(
        Guid? activeInstitutionId,
        string? storedSessionId,
        string? currentSessionId,
        string? actorPath,
        string? targetPath)
    {
        if (activeInstitutionId is not { } target || target == Guid.Empty)
            return null;

        // Ordinal ve büyük/küçük harfe DUYARLI: sid rastgele üretilmiş bir dizedir, harf
        // katlaması iki ayrı oturumu eşitleyebilirdi.
        if (string.IsNullOrEmpty(storedSessionId)
            || string.IsNullOrEmpty(currentSessionId)
            || !string.Equals(storedSessionId, currentSessionId, StringComparison.Ordinal))
        {
            return null;
        }

        return InstitutionScopePolicy.CanAccessByPath(actorPath, targetPath) ? target : null;
    }
}
