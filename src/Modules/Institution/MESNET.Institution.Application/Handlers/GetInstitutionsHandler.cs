using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Enums;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Kurum listesi aktörün <b>alt ağacıyla</b> sınırlıdır (ADR-0003 adım 6 + kurum hiyerarşisi).
///
/// <para>Bu sorgu <c>IInstitutionScoped</c> olamaz — hedef kurum istekte geçmez, sorulan zaten
/// "hangi kurumlar". Kapsam bu yüzden guard'la değil <b>süzmeyle</b> uygulanır.</para>
///
/// <para><b>Neden önemli:</b> <c>Institution</c> belgesi kiracının kendisidir ve kiracı damgası
/// taşımaz, yani conjoined kiracılık bu listeyi süzmez. Ölçüldü: süzme yokken bir okulun müdürü
/// diğer okulu listede görüyordu; kimlikle devam edip kaydını ve personel listesini de
/// okuyabiliyordu.</para>
///
/// <para><b>Sıralama artık ZORUNLU.</b> Bu sorgunun <c>ORDER BY</c>'ı yoktu ve Postgres
/// güncellenen satırı heap'te yerinden oynattığı için sıra iki çağrı arasında değişiyordu.
/// Ölçüldü (27.08.2026): kurumu olmayan platform aktörü için "listenin ilk satırı" her
/// yazmadan sonra başka bir okuldu; yönetim ekranı paleti yanlış okula yazdı.</para>
/// </summary>
public static class GetInstitutionsHandler
{
    public static async Task<PagedResult<InstitutionDto>> Handle(
        GetInstitutions query,
        IQuerySession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var scope = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        IQueryable<InstitutionRecord> queryable = session.Query<InstitutionRecord>();

        queryable = queryable.ApplyScope(scope);

        // Varsayılan OKUL: çağıranların çoğu okul listesi bekler. Süzgeçsiz bırakılsaydı
        // il/ilçe müdürlükleri açılır listelerde okul gibi görünürdü — sessizce.
        queryable = queryable.OfNodeType(InstitutionNodeType.Resolve(query.NodeType));

        if (query.ParentId is { } parentId)
            queryable = queryable.Where(i => i.ParentId == parentId);

        queryable = ApplySearchTerm(queryable, query.Search);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: i => i.FullName);

        var page = await queryable.ToPagedResultAsync(query, cancellationToken);
        var parentNames = await ResolveParentNamesAsync(session, page.Items, cancellationToken);

        return PagedResult<InstitutionDto>.Create(
            page.Items
                .Select(i => i.ToDto(i.ParentId is { } id && parentNames.TryGetValue(id, out var name) ? name : null))
                .ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    /// <summary>
    /// Ad ve kurum kodu araması.
    ///
    /// <para>Kod <c>int</c> olduğu için <c>ApplySearch</c> ile aranamaz (o yalnız string
    /// alanlarda çalışır). Terim sayıya çevrilebiliyorsa kodda <b>tam eşleşme</b> aranır:
    /// kurum kodu tam girilen bir kimliktir, parçası anlamlı değildir.</para>
    /// </summary>
    private static IQueryable<InstitutionRecord> ApplySearchTerm(
        IQueryable<InstitutionRecord> queryable, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return queryable;

        var term = search.Trim();

        if (int.TryParse(term, out var code))
            return queryable.Where(i => i.InstitutionCode == code);

        return queryable.ApplySearch(term, i => i.FullName);
    }

    /// <summary>
    /// Üst düğüm adlarını <b>toplu</b> okur. Satır başına okuma yapılsaydı 20 satırlık bir
    /// sayfa 21 sorgu ederdi (N+1).
    /// </summary>
    private static async Task<Dictionary<Guid, string>> ResolveParentNamesAsync(
        IQuerySession session, IReadOnlyList<InstitutionRecord> items, CancellationToken cancellationToken)
    {
        var parentIds = items
            .Select(i => i.ParentId)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        if (parentIds.Count == 0)
            return [];

        var parents = await session.LoadManyAsync<InstitutionRecord>(cancellationToken, parentIds);

        return parents.ToDictionary(p => p.Id, p => p.FullName);
    }
}
