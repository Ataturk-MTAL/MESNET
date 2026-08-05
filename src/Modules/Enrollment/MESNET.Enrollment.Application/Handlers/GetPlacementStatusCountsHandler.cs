using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class GetPlacementStatusCountsHandler
{
    public static async Task<PlacementStatusCountsResult> Handle(
        GetPlacementStatusCounts query, IQuerySession session, ICurrentUserService currentUser)
    {
        // Liste ile AYNI kapsam (sayım kullanıcı işletme filtresi taşımaz → businessIdFilter null).
        // Çözülemeyen kapsam boş sayımdır — liste boşken sayacın dolu olması çelişki olurdu.
        if (await PlacementQueryScope.ResolveAsync(currentUser, session, businessIdFilter: null)
            is not { } scope)
            return new PlacementStatusCountsResult([]);

        var (institutionId, teacherId, effectiveBusinessId) = scope;

        IQueryable<InternshipPlacement> queryable = session.Query<InternshipPlacement>();

        if (institutionId.HasValue)
            queryable = queryable.Where(p => p.InstitutionId == institutionId.Value);
        if (effectiveBusinessId.HasValue)
            queryable = queryable.Where(p => p.BusinessId == effectiveBusinessId.Value);
        if (teacherId.HasValue)
            queryable = queryable.Where(p => p.TeacherId == teacherId.Value);
        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(p => p.AcademicPeriodId == query.AcademicPeriodId.Value);
        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            queryable = queryable.Where(p => p.BranchCode == query.BranchCode);

        // StatusName (SmartEnum düz string kopyası) projeksiyonu güvenli; bellek-içi grupla
        // (Marten LINQ GroupBy tuzaklarından kaçın). Overview kapsamı sınırlı satır sayısıdır.
        var statuses = await queryable.Select(p => p.StatusName).ToListAsync();
        var counts = statuses
            .GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());

        return new PlacementStatusCountsResult(counts);
    }
}
