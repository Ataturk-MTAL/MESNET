using Marten;
using MESNET.Audit.Application.Dtos;
using MESNET.Audit.Application.Extensions;
using MESNET.Audit.Application.Queries;
using MESNET.Audit.Core.Entities;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;

namespace MESNET.Audit.Application.Handlers;

/// <summary>
/// Denetim izi listesi.
/// </summary>
/// <remarks>
/// <para><b>Yeni kapsam ekseni DOĞMAZ.</b> Kurum kapsamı A parçasındaki
/// <see cref="InstitutionScopePolicy.VisibleScope"/> ile aynıdır:
/// <c>SubjectInstitutionPath.StartsWith(okuyucununYolu)</c>. Marten
/// <c>string.StartsWith</c>'i SQL'de <c>LIKE 'önek%'</c> çevirir.</para>
///
/// <para><b>Kiracılık tek başına yetmez</b> ve bu yüzden yol süzgeci ZORUNLUDUR: kiracı
/// damgası satırı okulun içinde tutar, ama il yetkilisi bir gün (B parçası) birden çok
/// kiracıya erişince ayrım yalnız yoldan gelir.</para>
///
/// <para><b><c>OutcomeName</c> ile süzülür, <c>Outcome.Name</c> ile DEĞİL:</b> SmartEnum
/// JSON'a düz string yazılır; <c>data->'outcome'->>'Name'</c> her zaman NULL döner ve süzgeç
/// sessizce hiçbir şey bulmaz.</para>
/// </remarks>
public static class GetAuditEntriesHandler
{
    public static async Task<PagedResult<AuditEntryDto>> Handle(
        GetAuditEntries query,
        IQuerySession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.GetCurrentUser();

        IQueryable<AuditEntry> queryable = session.Query<AuditEntry>();

        queryable = ApplyScope(queryable, query, currentUser, actor?.UserId);

        if (query.ActorId is { } actorId)
            queryable = queryable.Where(e => e.ActorId == actorId);

        if (!string.IsNullOrWhiteSpace(query.CommandType))
            queryable = queryable.Where(e => e.CommandType == query.CommandType);

        if (!string.IsNullOrWhiteSpace(query.Outcome))
            queryable = queryable.Where(e => e.OutcomeName == query.Outcome);

        if (query.From is { } from)
            queryable = queryable.Where(e => e.OccurredAt >= from);

        if (query.To is { } to)
            queryable = queryable.Where(e => e.OccurredAt <= to);

        if (query.CrossedTenantBoundary is { } crossed)
            queryable = queryable.Where(e => e.CrossedTenantBoundary == crossed);

        queryable = queryable.ApplySearch(query.Search, e => e.ActorName, e => e.CommandLabel);

        // Sıralama ZORUNLU: sırasız liste her yazmadan sonra kayar (Postgres güncellenen
        // satırı heap'te yerinden oynatır). Denetim izinde varsayılan yeniden eskiye.
        queryable = queryable.ApplySort(
            query.SortBy, descending: query.SortBy is null || query.Descending,
            defaultSort: e => e.OccurredAt);

        var page = await queryable.ToPagedResultAsync(query, cancellationToken);

        return PagedResult<AuditEntryDto>.Create(
            page.Items.Select(e => e.ToDto()).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    /// <summary>
    /// Kapsam daraltması. İki mod vardır ve <b>ikisi de sunucudadır</b>; istemcinin gönderdiği
    /// <c>scope</c> bir NİYETTİR, yetki değil — <c>institution</c> modunun izni uç seviyesinde
    /// kontrol edilir (<c>audit:view:institution</c>).
    /// </summary>
    private static IQueryable<AuditEntry> ApplyScope(
        IQueryable<AuditEntry> queryable,
        GetAuditEntries query,
        ICurrentUserService currentUser,
        Guid? actorUserId)
    {
        if (!string.Equals(query.Scope, GetAuditEntries.ScopeInstitution, StringComparison.Ordinal))
        {
            // "Kendi işlemlerim". YANLIŞ DEĞİŞMEZ İDDİASI DÜZELTİLDİ (madde 4): Guid.Empty
            // "hiçbir satırla eşleşmez" DEĞİL — AuditMiddleware.Before
            // (ActorId = actor?.UserId ?? Guid.Empty) ve CurrentUserService (sub claim'i
            // çözülemeyince UserId = Guid.Empty) tam da Guid.Empty aktörlü satır yazabilir.
            // Kimliği çözülemeyen bu istekte de aktörUserId Guid.Empty'ye düşer, yani
            // kimliksiz istek KENDİ ürettiği Guid.Empty'li satırları görür — üstelik başka
            // kimliksiz isteklerin de Guid.Empty'li satırlarını görebilir (aktörler birbirinin
            // izini görür). Olasılık düşük (bu yol yalnız aktör çözülemediğinde çalışır) ve
            // sonuç yine kiracıyla sınırlıdır (Marten conjoined kiracılık satırı filtreler),
            // ama "hiçbir şey görmek" iddiası yanlıştı.
            var userId = actorUserId ?? Guid.Empty;
            return queryable.Where(e => e.ActorId == userId);
        }

        var scope = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        if (scope.Unrestricted)
            return queryable;

        if (scope.PathPrefix is { } prefix)
        {
            return queryable.Where(e =>
                e.SubjectInstitutionPath != null && e.SubjectInstitutionPath.StartsWith(prefix));
        }

        // Yol yok: kimliğe düş — geçiş ucu koşmamış kurumda bugünkü davranış korunur.
        var institutionId = scope.InstitutionId ?? Guid.Empty;
        return queryable.Where(e => e.SubjectInstitutionId == institutionId);
    }
}
