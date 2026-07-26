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
    IReadOnlyList<string>? BranchCodes = null);
