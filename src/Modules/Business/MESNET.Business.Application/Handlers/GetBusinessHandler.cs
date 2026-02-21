using Marten;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Errors;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Application.Queries;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class GetBusinessHandler
{
    public static async Task<BusinessDto> Handle(GetBusiness query, IQuerySession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(query.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(query.BusinessId));

        return business.ToDto();
    }
}
