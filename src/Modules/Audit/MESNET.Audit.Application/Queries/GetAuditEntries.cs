using MESNET.Common.Shared.Pagination;

namespace MESNET.Audit.Application.Queries;

/// <summary>
/// Denetim izi listesi.
/// </summary>
/// <param name="Scope">
/// <c>"mine"</c> = yalnız aktörün kendi işlemleri (izin GEREKTİRMEZ — kendi geçmişini görmek
/// bir yetki sorusu değildir). <c>"institution"</c> = kurum ağacı (yol öneki), uç seviyesinde
/// <c>audit:view:institution</c> ile korunur.
/// </param>
public sealed record GetAuditEntries(
    string Scope,
    Guid? ActorId = null,
    string? CommandType = null,
    string? Outcome = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    bool? CrossedTenantBoundary = null) : PagedQuery
{
    public const string ScopeMine = "mine";
    public const string ScopeInstitution = "institution";
}
