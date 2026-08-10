namespace MESNET.Common.Shared.Security;

/// <summary>
/// Bir kullanıcının <b>kurum (kiracı) bağını</b> kimin değiştirebileceğine karar verir
/// (ADR-0003 adım 2).
///
/// <para><b>İzin erişimi açar, kapsamı belirlemez</b> (ADR-0001). Ucu koruyan izin
/// <c>user:roles:manage</c>'dır — alan kapsamı ve rol atamasıyla aynı yetki seviyesi, çünkü
/// üçü de <i>yetki kapsamı</i> kararıdır. Ama "hangi kuruma bağlanabilir" sorusu izinle değil
/// aktörün <b>kendi kurum kapsamıyla</b> cevaplanır ve o kapsam istekten değil token
/// claim'inden okunur.</para>
///
/// <para><b>Üç kural:</b></para>
/// <list type="bullet">
///   <item><b>Kapsamsız aktör yazamaz.</b> Kendi kurumu olmayan kullanıcı, kiracı anahtarı
///   dağıtamaz — aksi hâlde kapsamsızlık sınırsızlık anlamına gelirdi.</item>
///   <item><b>Başka kuruma bağlı kullanıcıya dokunulamaz.</b> Devralma tek taraflı bir işlem
///   değildir; önce mevcut kurum bağı çözmelidir.</item>
///   <item><b>Hedef ya aktörün kendi kurumudur ya da bağın çözülmesidir.</b> Kullanıcıyı
///   başka bir kuruma yazmak hiçbir aktörün yetkisinde değildir.</item>
/// </list>
///
/// <para><b>Neden bugün yazılıyor:</b> Faz 1'de tek kurum var, yani kural pratikte hep
/// sağlanıyor. Ama kiracı anahtarını yazan uç bu kontrol olmadan açılırsa, çok kiracılığa
/// geçildiği gün (bkz. #149) her okul yöneticisi başka okulun kullanıcısını kendi kiracısına
/// çekebilirdi ve bunu fark ettiren hiçbir sinyal olmazdı.</para>
/// </summary>
public static class UserInstitutionScopePolicy
{
    /// <param name="actorInstitutionId">İşlemi yapanın kurum kapsamı — token claim'inden.</param>
    /// <param name="currentInstitutionId">Hedef kullanıcının bugünkü kurum bağı.</param>
    /// <param name="targetInstitutionId">Yazılmak istenen bağ; <c>null</c> = bağı çöz.</param>
    public static bool CanAssign(
        Guid? actorInstitutionId, Guid? currentInstitutionId, Guid? targetInstitutionId)
    {
        if (Normalize(actorInstitutionId) is not { } actor)
            return false;

        if (Normalize(currentInstitutionId) is { } current && current != actor)
            return false;

        return Normalize(targetInstitutionId) is not { } target || target == actor;
    }

    /// <summary>Boş Guid ile <c>null</c> aynı anlama gelir: bağ yok.</summary>
    private static Guid? Normalize(Guid? value) =>
        value is { } id && id != Guid.Empty ? id : null;
}
