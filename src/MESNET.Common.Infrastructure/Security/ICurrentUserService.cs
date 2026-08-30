using MESNET.Common.Shared.Security;

namespace MESNET.Common.Infrastructure.Security;

public interface ICurrentUserService
{
    UserContext? GetCurrentUser();
    Guid GetUserId();
    string GetFullName();
    bool HasPermission(string permission);
    bool IsInRole(string role);

    /// <summary>
    /// Kullanıcının sorumlu olduğu alan (branş) kodları — <c>branch_codes</c> claim'i (#126).
    /// Kapsam kararı için kullanılır; erişim kararı için değil (o permission'ın işidir).
    /// Bilgi yoksa boş liste döner — boş liste "hiçbir alana yazamaz" demektir.
    /// </summary>
    IReadOnlyList<string> GetBranchCodes();

    /// <summary>
    /// Velinin bağlı olduğu öğrenciler — <c>linked_student_ids</c> claim'i (#174).
    /// Kapsam kararı için kullanılır; erişim kararı için değil (o permission'ın işidir).
    /// Bilgi yoksa boş liste döner — boş liste "hiçbir öğrenciye erişemez" demektir.
    /// </summary>
    IReadOnlyList<Guid> GetLinkedStudentIds();

    /// <summary>
    /// Aktörün kurum ağacındaki yolu — <c>institution_path</c> claim'i.
    /// Kapsam kararı için kullanılır; erişim kararı için değil (o permission'ın işidir).
    /// Bilgi yoksa <c>null</c> döner ve kapsam kimlik eşitliğine düşer.
    /// </summary>
    string? GetInstitutionPath();
}
