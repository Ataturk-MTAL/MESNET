using Marten;
using MESNET.Contract.Application.Dtos;
using MESNET.Contract.Application.Extensions;
using MESNET.Contract.Application.Queries;
using MESNET.Contract.Core.Aggregates;

namespace MESNET.Contract.Application.Handlers;

public static class GetContractHandler
{
    public static async Task<InternshipContractDto?> Handle(GetContract query, IQuerySession session)
    {
        var contract = await session.Events.AggregateStreamAsync<InternshipContract>(query.ContractId);
        return contract?.ToDto();
    }
}
