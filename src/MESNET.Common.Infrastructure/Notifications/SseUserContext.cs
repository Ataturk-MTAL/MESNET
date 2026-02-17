namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// SSE bağlantısı açan kullanıcının JWT claim bilgileri.
/// Bağlantı açılırken parse edilir ve Channel ile ilişkilendirilir.
/// </summary>
public sealed record SseUserContext(
    Guid UserId,
    string FullName,
    Guid? InstitutionId,
    Guid? BusinessId,
    Guid? StudentId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
