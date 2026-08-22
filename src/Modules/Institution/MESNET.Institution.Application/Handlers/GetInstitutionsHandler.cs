using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;

namespace MESNET.Institution.Application.Handlers;

/// <summary>
/// Kurum listesi <b>aktörün kendi okuluyla</b> sınırlıdır (ADR-0003 adım 6).
///
/// <para>Bu sorgu <c>IInstitutionScoped</c> olamaz — hedef kurum istekte geçmez, sorulan zaten
/// "hangi kurumlar". Kapsam bu yüzden guard'la değil <b>süzmeyle</b> uygulanır.</para>
///
/// <para><b>Neden önemli:</b> <c>Institution</c> belgesi kiracının kendisidir ve kiracı damgası
/// taşımaz, yani conjoined kiracılık bu listeyi süzmez. Ölçüldü: süzme yokken bir okulun müdürü
/// diğer okulu listede görüyordu; kimlikle devam edip kaydını ve personel listesini de
/// okuyabiliyordu.</para>
///
/// <para><b>Kapsamsız aktör boş liste görür</b>, her şeyi değil:
/// <see cref="InstitutionScopePolicy.VisibleInstitutionFilter"/> onun için hiçbir kurumla
/// eşleşmeyen bir kimlik döndürür.</para>
/// </summary>
public static class GetInstitutionsHandler
{
    public static async Task<List<InstitutionDto>> Handle(
        GetInstitutions query, IQuerySession session, ICurrentUserService currentUser)
    {
        var filter = InstitutionScopePolicy.VisibleInstitutionFilter(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        IQueryable<Core.Entities.Institution> queryable = session.Query<Core.Entities.Institution>();

        // null = daraltma yok (yalnız platform aktörü); aksi hâlde tek kuruma indirilir.
        if (filter is { } institutionId)
            queryable = queryable.Where(i => i.Id == institutionId);

        var institutions = await queryable.ToListAsync();
        return institutions.Select(i => i.ToDto()).ToList();
    }
}
