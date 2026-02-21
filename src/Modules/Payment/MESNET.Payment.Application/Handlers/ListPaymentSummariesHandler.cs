using Marten;
using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Application.Extensions;
using MESNET.Payment.Application.Queries;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;

namespace MESNET.Payment.Application.Handlers;

public static class ListPaymentSummariesHandler
{
    public static async Task<IReadOnlyList<PaymentSummaryDto>> Handle(ListPaymentSummaries query, IQuerySession session)
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

        var summaries = await q.ToListAsync();

        // SmartEnum LINQ kısıtı: in-memory filtrele
        if (!string.IsNullOrWhiteSpace(query.Phase) && PaymentPhase.TryFromName(query.Phase, out var phase))
            summaries = summaries.Where(p => p.Phase.Name == phase.Name).ToList();

        return summaries.Select(s => s.ToDto()).ToList();
    }
}
