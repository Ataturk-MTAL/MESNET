using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// Veli kapsamı guard'ı (#174) — <c>BranchScopeGuard</c> (#126) deseninin aynısı.
///
/// <para><b>Kural:</b> bağ kaydı olan bir kullanıcı <b>yalnız</b> bağlı olduğu öğrencinin
/// verisine dokunabilir. Bağı olmayan kullanıcılar (okul ve işletme tarafı) bu kontrolden
/// etkilenmez — onların kapsamı kurum/işletme claim'lerinden gelir.</para>
///
/// <para><b>Rol adına bakılmaz</b> (ADR-0001): "veli mi" diye sorulmaz, "bağlı öğrencisi var mı"
/// diye sorulur. Rol yeniden adlandırılsa da kayıt yerinde kalır. Aynı sebeple guard tek yerdedir:
/// her uç kendi kontrolünü yazsaydı biri unutulduğunda kapsam sessizce açılırdı.</para>
///
/// <para><b>Kapsam istekten ALINMAZ:</b> öğrenci kimliği çağıranın gönderdiği gövdeden değil,
/// sunucuda çözülmüş kayıttan (devamsızlık kaydı, staj, yerleştirme) okunmalıdır. Aksi hâlde
/// veli başka bir öğrencinin kimliğini göndererek kontrolü kendi lehine çevirirdi.</para>
/// </summary>
public static class ParentScopeGuard
{
    /// <summary>
    /// Kullanıcı bu öğrenciye dokunabilir mi. Bağı olmayan kullanıcı için her zaman
    /// <c>true</c> — bu guard yalnız bağ kapsamını uygular, erişim kararını değil.
    /// </summary>
    public static bool CanAccessStudent(ICurrentUserService currentUser, Guid studentId)
    {
        var linked = currentUser.GetLinkedStudentIds();

        // Bağ kaydı yoksa bu kullanıcı bağ kapsamına tabi değildir (okul/işletme tarafı).
        if (!ParentScopePolicy.HasLinkedStudents(linked))
            return true;

        return ParentScopePolicy.CanAccessStudent(linked, studentId);
    }

    /// <summary>
    /// İhlalde <see cref="DomainException"/> fırlatır (HTTP 422). Yazma yollarında
    /// <see cref="CanAccessStudent"/> yerine bu kullanılır ki hata mesajı tek yerde kalsın.
    /// </summary>
    public static void EnsureCanAccessStudent(ICurrentUserService currentUser, Guid studentId)
    {
        if (CanAccessStudent(currentUser, studentId)) return;

        throw new DomainException(
            "PARENT_SCOPE_VIOLATION",
            "Bu işlem yalnız bağlı olduğunuz öğrenci için yapılabilir.");
    }
}
