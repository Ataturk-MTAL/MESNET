using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Policies;

namespace MESNET.Enrollment.Application.Handlers;

/// <summary>
/// Yerleştirme sorgularının kapsam daraltması. Liste ve sayım handler'larının AYNI kapsamı
/// uygulaması için tek kaynak.
///
/// <para>Karar <see cref="PlacementScopePolicy"/>'dedir ve testle kilitlidir (#184); burada
/// yalnız G/Ç yapılır — izin okuma, claim okuma, öğretmen kaydı arama.</para>
/// </summary>
internal static class PlacementQueryScope
{
    /// <summary>
    /// Kapsamı çözer. <c>null</c> dönerse kullanıcının göreceği kayıt <b>yoktur</b> — çağıran
    /// boş sonuç döndürmelidir.
    /// </summary>
    public static async Task<PlacementScope?> ResolveAsync(
        ICurrentUserService currentUser, IQuerySession session, Guid? businessIdFilter)
    {
        var user = currentUser.GetCurrentUser();

        // `institution:view` okul yönetimi kümesini tam olarak tanımlar: müdür (wildcard),
        // müdür yardımcısı ve kurum personeli. Öğretmende yoktur.
        var hasInstitutionWideView = currentUser.HasPermission(Permissions.Institution.View);

        // Öğretmen kaydı yalnız üst basamaklar tutmadığında aranır — karar sırası
        // PlacementScopePolicy'de, buradaki atlama yalnız gereksiz sorguyu önler.
        Guid? coordinatorTeacherId = null;
        if (!hasInstitutionWideView && user is not null && user.BusinessId is null)
        {
            var teacher = await session.Query<TeacherProfile>()
                .FirstOrDefaultAsync(t => t.KeycloakUserId == user.UserId);
            coordinatorTeacherId = teacher?.Id;
        }

        return PlacementScopePolicy.Resolve(
            hasInstitutionWideView,
            user?.InstitutionId,
            user?.BusinessId,
            businessIdFilter,
            coordinatorTeacherId);
    }
}
