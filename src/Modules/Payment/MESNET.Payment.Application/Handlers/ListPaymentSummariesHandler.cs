using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Application.Extensions;
using MESNET.Payment.Application.Queries;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;

namespace MESNET.Payment.Application.Handlers;

public static class ListPaymentSummariesHandler
{
    public static async Task<PagedResult<PaymentSummaryDto>> Handle(ListPaymentSummaries query, IQuerySession session)
    {
        IQueryable<PaymentSummary> q = session.Query<PaymentSummary>();

        if (query.StudentId.HasValue)
            q = q.Where(p => p.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            q = q.Where(p => p.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            q = q.Where(p => p.InstitutionId == query.InstitutionId.Value);

        if (!string.IsNullOrWhiteSpace(query.Month))
            q = q.Where(p => p.Month == query.Month);

        if (!string.IsNullOrWhiteSpace(query.Phase) && PaymentPhase.TryFromName(query.Phase, out var phase))
            q = q.Where(p => p.Phase.Name == phase.Name);

        q = q.ApplySort(query.SortBy, query.Descending, defaultSort: p => p.Month);

        return await q.ToPagedResultAsync(query, s => s.ToDto());
    }
}
