using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Application.Extensions;
using MESNET.Payment.Application.Queries;
using MESNET.Payment.Core.Entities;

namespace MESNET.Payment.Application.Handlers;

public static class GetPaymentSummaryHandler
{
    public static async Task<PaymentSummaryDto> Handle(GetPaymentSummary query, IQuerySession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(query.Id);
        if (summary is null)
            throw new DomainException(PaymentErrors.NotFound(query.Id));

        return summary.ToDto();
    }
}
