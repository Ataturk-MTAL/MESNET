using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ReadModels;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Alt ağaçta <c>institution:manage</c> taşıyan etkin kullanıcısı olmayan okullar.
///
/// <para><b>Sorgu iki adımlı ve NEGATİF yöndedir.</b> Marten join yapmaz. Önce YÖNETİLEN kurum
/// kimlikleri toplanır, sonra kurum listesi o kümenin DIŞINDA kalanlara daraltılır.</para>
///
/// <para><b>Neden pozitif yön değil:</b> "yöneticisiz kurumların kimliklerini topla" demek her
/// kurum için bir read-model satırının var olmasını gerektirirdi; hiç kullanıcı olayı görmemiş
/// kurum o listede hiç doğmazdı — aranan kurum tam olarak o.</para>
///
/// <para><b>Neden sayfalama ikinci adımda:</b> önce kurumları sayfalayıp sonra bellekte süzmek
/// sayfa boyutlarını yanlışlardı — 20 satırlık sayfadan 3'ü kalırsa istemci "3 sonuç var"
/// sanır.</para>
/// </summary>
public static class GetUnmanagedInstitutionsHandler
{
    public static async Task<PagedResult<InstitutionDto>> Handle(
        GetUnmanagedInstitutions query,
        IQuerySession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        // 1. adım: yönetilen kurumlar.
        var managedIds = await session.Query<InstitutionManagerLink>()
            .Where(l => l.IsEnabled && l.HasManagePermission && l.InstitutionId != null)
            .Select(l => l.InstitutionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var scope = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        IQueryable<InstitutionRecord> queryable = session.Query<InstitutionRecord>();

        queryable = ApplyScope(queryable, scope);

        // Yalnız OKUL: il/ilçe müdürlüğünün "yöneticisi" bu kartın konusu değildir.
        queryable = queryable.OfNodeType(InstitutionNodeType.School);

        // 2. adım: negatif süzgeç. Boş kümede Contains her satırı geçirir (doğru davranış:
        // hiçbir okul yönetilmiyorsa hepsi listelenir), ayrıca ele alınmasına gerek yok.
        if (managedIds.Count > 0)
            queryable = queryable.Where(i => !managedIds.Contains(i.Id));

        queryable = queryable.ApplySearch(query.Search, i => i.FullName);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: i => i.FullName);

        var page = await queryable.ToPagedResultAsync(query, cancellationToken);

        return PagedResult<InstitutionDto>.Create(
            page.Items.Select(i => i.ToDto(null)).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    /// <summary>
    /// Kapsam daraltması — <c>GetInstitutionsHandler</c> ile AYNI karardan (<see
    /// cref="InstitutionVisibility"/>) beslenir; karar burada TEKRARLANMAZ.
    /// </summary>
    private static IQueryable<InstitutionRecord> ApplyScope(
        IQueryable<InstitutionRecord> queryable, InstitutionVisibility scope)
    {
        if (scope.Unrestricted)
            return queryable;

        if (scope.PathPrefix is { } prefix)
            return queryable.Where(i => i.Path != null && i.Path.StartsWith(prefix));

        var institutionId = scope.InstitutionId ?? Guid.Empty;
        return queryable.Where(i => i.Id == institutionId);
    }
}
