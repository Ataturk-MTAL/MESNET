using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Contract.Application.Dtos;
using MESNET.Contract.Application.Extensions;
using MESNET.Contract.Application.Queries;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;

namespace MESNET.Contract.Application.Handlers;

public static class ListContractsHandler
{
    public static async Task<PagedResult<InternshipContractDto>> Handle(
        ListContracts query, IQuerySession session)
    {
        IQueryable<InternshipContract> queryable = session.Query<InternshipContract>();

        if (query.StudentId.HasValue)
            queryable = queryable.Where(c => c.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(c => c.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(c => c.InstitutionId == query.InstitutionId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(c => c.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            ContractStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(c => c.StatusName == status.Name);

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: c => c.CreatedAt);

        return await queryable.ToPagedResultAsync(query, c => c.ToDto());
    }
}
