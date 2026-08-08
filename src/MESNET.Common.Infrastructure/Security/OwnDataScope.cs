using MESNET.Common.Shared.Security;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// "Kendi verisi" kapsamının tek yerden çözümü (#182).
///
/// <para>Bir listeleme ucu hem okul tarafına hem veri sahibine açıksa, <b>kimin ne göreceğine</b>
/// izin değil kapsam karar verir (ADR-0001). Merdiven her modülde aynı sırayla işler:</para>
///
/// <list type="number">
///   <item><b>Geniş görüntüleme izni</b> varsa (ör. <c>attendance:view</c>) → kapsam
///         daraltılmaz; okul ve işletme tarafının bugünkü davranışı korunur.</item>
///   <item><b>Veli</b> — <c>linked_student_ids</c> kaydı varsa yalnız bağlı öğrenciler (#174).</item>
///   <item><b>Öğrenci</b> — <c>student_id</c> claim'i varsa yalnız kendisi.</item>
///   <item>Hiçbiri yoksa → <b>boş küme</b>. Kapsamsız kullanıcıya tüm kurumun verisini
///         açmaktansa hiçbir şey göstermek doğrudur.</item>
/// </list>
///
/// <para><b>Neden ayrı sınıf:</b> aynı merdiven devamsızlık, staj ve ücret listelerinde
/// tekrarlanıyor. Üç yere kopyalansaydı biri güncellenip diğeri unutulduğunda sapma sessiz
/// olurdu — ve sessiz kapsam sapması, açılan veriyi kimsenin fark etmediği türden bir hatadır.</para>
/// </summary>
public static class OwnDataScope
{
    /// <summary>Kapsam çözümünün sonucu.</summary>
    /// <param name="IsUnrestricted">Geniş izin var — sorgu daraltılmaz.</param>
    /// <param name="StudentIds">Daraltma uygulanacak öğrenci kimlikleri. Boşsa sonuç boş olmalıdır.</param>
    /// <param name="BusinessId">
    /// Daraltma uygulanacak işletme (#191). Yalnız <c>includeBusinessScope</c> istendiğinde
    /// dolar. <see cref="StudentIds"/> ile <b>birleşir</b>, onu ezmez: hem veli hem işletme
    /// yetkilisi olan kullanıcı ikisini de görür.
    /// </param>
    public readonly record struct Result(
        bool IsUnrestricted, IReadOnlyList<Guid> StudentIds, Guid? BusinessId = null)
    {
        /// <summary>Kapsam çözülemedi — çağıran <b>boş sonuç</b> döndürmelidir.</summary>
        public bool IsEmpty => !IsUnrestricted && StudentIds.Count == 0 && BusinessId is null;
    }

    /// <summary>
    /// Merdiveni yürütür. <paramref name="broadViewPermission"/> modülün geniş görüntüleme
    /// iznidir (<c>attendance:view</c>, <c>internship:view</c>, <c>salary:view</c>).
    /// </summary>
    /// <param name="includeBusinessScope">
    /// İşletme basamağı da değerlendirilsin mi (#191). <b>Varsayılan KAPALI</b> ve bu bilinçli:
    /// işletme yetkilisinin kendi stajlarını görmesi yalnız staj listesinde isteniyor;
    /// devamsızlık ve ücret listelerinde aynı genişleme istenmiyor. Varsayılan açık olsaydı o
    /// iki uç sessizce genişlerdi — kimsenin talep etmediği bir erişim, kimsenin fark etmediği
    /// bir açıktır.
    /// </param>
    public static Result Resolve(
        ICurrentUserService currentUser, string broadViewPermission, bool includeBusinessScope = false)
    {
        if (currentUser.HasPermission(broadViewPermission))
            return new Result(IsUnrestricted: true, []);

        // İşletme kimliği claim'den okunur, istekten DEĞİL (ADR-0001).
        Guid? businessId = null;
        if (includeBusinessScope
            && currentUser.GetCurrentUser()?.BusinessId is { } b && b != Guid.Empty)
            businessId = b;

        var linked = currentUser.GetLinkedStudentIds();
        if (ParentScopePolicy.HasLinkedStudents(linked))
            return new Result(IsUnrestricted: false, linked, businessId);

        var ownStudentId = currentUser.GetCurrentUser()?.StudentId ?? Guid.Empty;
        if (ownStudentId != Guid.Empty)
            return new Result(IsUnrestricted: false, [ownStudentId], businessId);

        return new Result(IsUnrestricted: false, [], businessId);
    }
}
