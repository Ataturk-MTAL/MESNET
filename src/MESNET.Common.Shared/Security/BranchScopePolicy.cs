namespace MESNET.Common.Shared.Security;

/// <summary>
/// Alan (branş) kapsamı kararı — saf mantık (#126).
///
/// <para>Permission <b>erişimi</b> açar, kapsamı belirlemez. "Hangi alanın verisi" sorusunun
/// yanıtı buradadır: istekteki alan kodu kullanıcının sorumlu olduğu alanlar arasında mı?</para>
///
/// <para>HttpContext, Marten veya DI bağımlılığı yoktur; girdiler dışarıdan verilir ki
/// karar birim testlenebilsin.</para>
/// </summary>
public static class BranchScopePolicy
{
    /// <summary>
    /// Kullanıcı verilen alana <b>yazabilir mi</b>?
    /// </summary>
    /// <param name="requestedBranchCode">
    /// İsteğin hedeflediği alan kodu. Çözümlenmiş satırın alan kodu tercih edilir —
    /// istekten gelen ham parametre boş bırakılarak kontrol atlatılamasın.
    /// </param>
    /// <param name="userBranchCodes">Kullanıcının sorumlu olduğu alan kodları (token claim'i).</param>
    /// <param name="hasAllBranchesPermission">
    /// Kurum geneli muafiyet izni (<see cref="Permissions.Institution.AllBranches"/>).
    /// Müdür ve müdür yardımcısında vardır; alan şefinde yoktur.
    /// </param>
    /// <returns>
    /// Muafiyet varsa her zaman <c>true</c>. Muafiyet yoksa: hedef alan boşsa (kapsam
    /// bilinmiyor) veya kullanıcının alan listesi boşsa <c>false</c> — kapsamı bilinmeyen
    /// yazma isteği kabul edilmez.
    /// </returns>
    /// <remarks>
    /// <b>Karar sırası önemlidir: önce muafiyet, sonra liste.</b> Herkesin branş kodu olmak
    /// zorunda değildir — okul müdürü ve müdür yardımcısı hiçbir alana bağlı değildir ve bu
    /// bir veri eksikliği DEĞİL, doğru durumdur. Liste önce kontrol edilip boşsa reddedilseydi
    /// yöneticiler kilitlenirdi. Boş liste bu yüzden hata da değildir: yalnız muafiyeti
    /// olmayan kullanıcı için "hiçbir alana yazamaz" anlamına gelir.
    /// </remarks>
    public static bool CanWrite(
        string? requestedBranchCode,
        IReadOnlyList<string>? userBranchCodes,
        bool hasAllBranchesPermission)
    {
        // Muafiyet varsa alan listesine HİÇ bakılmaz — boş olması hiçbir şeyi değiştirmez.
        if (hasAllBranchesPermission)
            return true;

        if (string.IsNullOrWhiteSpace(requestedBranchCode))
            return false;

        if (userBranchCodes is null || userBranchCodes.Count == 0)
            return false;

        return userBranchCodes.Any(code =>
            !string.IsNullOrWhiteSpace(code) &&
            string.Equals(code.Trim(), requestedBranchCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
