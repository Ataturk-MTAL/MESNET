using Marten;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Application.Queries;

namespace MESNET.Business.Application.Handlers;

public static class GetBusinessHandler
{
    public static async Task<BusinessDto?> Handle(GetBusiness query, IQuerySession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(query.BusinessId);
        return business?.ToDto();
    }
}
