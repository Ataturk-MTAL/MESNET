namespace MESNET.Common.Shared.Security;

/// <summary>
/// İl/ilçe yetkilisinin alt ağacındaki bir okula <b>ilk yöneticiyi</b> bağlayabilmesinin
/// koşulu (B parçası).
/// </summary>
/// <remarks>
/// <para><b>Neden ayrı bir politika, <c>UserInstitutionScopePolicy.CanAssign</c>'a dal
/// eklemek değil:</b> o politika "aktör kendi kurumuna bağlar" kuralını taşıyor ve tek bir
/// cümlede okunabiliyor. Bootstrap farklı bir sorudur — tıkanıklık var mı — ve girdileri de
/// farklıdır. İkisini birleştirmek beş parametreli, iki ayrı gerçeği kodlayan bir yüklem
/// üretirdi.</para>
///
/// <para><b>Yeni okulun ilk kullanıcısı sorunu A parçasında da vardı</b> ve orada
/// <c>platform:tenant:manage</c> istisnasıyla çözülmüştü. Bu politika aynı boşluğu il
/// yetkilisi için, çok daha dar bir kapıyla açar: yalnız kendi alt ağacında ve yalnız
/// yöneticisi olmayan okulda.</para>
/// </remarks>
public static class InstitutionBootstrapPolicy
{
    /// <param name="hasBootstrapPermission"><c>directorate:institution-bootstrap</c>.</param>
    /// <param name="targetInActorSubtree">
    /// Hedef kurum aktörün yol önekinin altında mı — <c>InstitutionScopePolicy.CanAccessByPath</c>.
    /// </param>
    /// <param name="targetHasManager">
    /// Hedef kurumun <b>etkin</b> bir yöneticisi var mı. Varsa tıkanıklık yoktur ve müdahale
    /// yolu kapalıdır.
    /// </param>
    public static bool CanBootstrap(
        bool hasBootstrapPermission, bool targetInActorSubtree, bool targetHasManager)
        => hasBootstrapPermission && targetInActorSubtree && !targetHasManager;
}
