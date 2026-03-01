using Marten;
using MESNET.Common.Shared;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Application.Extensions;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Core.Entities;

namespace MESNET.Internship.Application.Handlers;

public static class GetInternshipHandler
{
    public static async Task<InternshipSummaryDto> Handle(
        GetInternship query, IQuerySession session)
    {
        var summary = await session.LoadAsync<InternshipSummary>(query.InternshipId);
        if (summary is null)
            throw new DomainException(InternshipErrors.NotFound(query.InternshipId));

        return summary.ToDto();
    }
}
