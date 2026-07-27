using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

/// <summary>
/// Yerleştirme sorgularının yetki-kapsam daraltması (kurum + rol bazlı). Liste ve sayım
/// handler'larının AYNI kapsamı uygulaması için tek kaynak — kapsam kuralı değişirse tek yerde.
/// </summary>
internal static class PlacementQueryScope
{
    public static async Task<(Guid? InstitutionId, Guid? TeacherId, Guid? EffectiveBusinessId)> ResolveAsync(
        ICurrentUserService currentUser, IQuerySession session, Guid? businessIdFilter)
    {
        var user = currentUser.GetCurrentUser();
        var institutionId = user?.InstitutionId;

        // Teacher (kurum yöneticisi/personeli değilse) yalnız koordine ettiği öğrencileri görür.
        // NOT: bu kontrol rol adına bakar — bilinen teknik borç (bkz. CLAUDE.md → "Bu kuralın
        // bilinen istisnaları"), permission'a taşınmalıdır. #129 ile müdür yardımcısı ayrı role
        // (DeputyDirector) çıktığı için liste genişletildi; eklenmeseydi öğretmenliği de olan
        // bir müdür yardımcısı kurum geneli yerleştirme görünürlüğünü sessizce kaybederdi.
        Guid? teacherId = null;
        if (user is not null
            && currentUser.IsInRole(MesnetRoles.Teacher)
            && !currentUser.IsInRole(MesnetRoles.InstitutionManager)
            && !currentUser.IsInRole(MesnetRoles.DeputyDirector)
            && !currentUser.IsInRole(MesnetRoles.InstitutionStaff))
        {
            var teacher = await session.Query<TeacherProfile>()
                .FirstOrDefaultAsync(t => t.KeycloakUserId == user.UserId);
            teacherId = teacher?.Id;
        }

        // CompanyManager yalnız kendi işletmesini görür (kullanıcı filtresini override eder).
        var effectiveBusinessId = businessIdFilter;
        if (currentUser.IsInRole(MesnetRoles.CompanyManager) && user?.BusinessId.HasValue == true)
            effectiveBusinessId = user.BusinessId;

        return (institutionId, teacherId, effectiveBusinessId);
    }
}
