namespace MESNET.Common.Shared.Security;

/// <summary>
/// JWT claim'lerden oluşturulan kullanıcı bağlamı.
/// Tüm modüller tarafından ortaklaşa kullanılır.
/// </summary>
public sealed record UserContext(
    Guid UserId,
    string FullName,
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    Guid? StudentId = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<string>? Permissions = null,
    /// <summary>
    /// Kullanıcının sorumlu olduğu alan (branş) kodları — <c>branch_codes</c> claim'i (#126).
    /// Bir alan şefi birden çok alandan sorumlu olabildiği için listedir.
    /// </summary>
    IReadOnlyList<string>? BranchCodes = null,
    /// <summary>
    /// Velinin bağlı olduğu öğrenciler — <c>linked_student_ids</c> claim'i (#174).
    /// Bir veli birden çok öğrenciye bağlı olabildiği için listedir. Boş liste "bağ kurulmamış"
    /// demektir ve hiçbir öğrenciye erişim doğurmaz.
    /// </summary>
    IReadOnlyList<Guid>? LinkedStudentIds = null);
