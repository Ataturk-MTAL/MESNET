using Marten;
using MESNET.Contract.Application.Dtos;
using MESNET.Contract.Application.Extensions;
using MESNET.Contract.Application.Queries;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;

namespace MESNET.Contract.Application.Handlers;

public static class ListContractsHandler
{
    public static async Task<IReadOnlyList<InternshipContractDto>> Handle(
        ListContracts query, IQuerySession session)
    {
        IQueryable<InternshipContract> queryable = session.Query<InternshipContract>();

        if (query.StudentId.HasValue)
            queryable = queryable.Where(c => c.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(c => c.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(c => c.InstitutionId == query.InstitutionId.Value);

        var contracts = await queryable.ToListAsync();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            ContractStatus.TryFromName(query.Status, true, out var status))
            contracts = contracts.Where(c => c.Status.Name == status.Name).ToList();

        return contracts.Select(c => c.ToDto()).ToList();
    }
}
