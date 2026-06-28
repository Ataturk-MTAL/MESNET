using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Extensions;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Enums;

namespace MESNET.Internship.Application.Handlers;

public static class ListInternshipsHandler
{
    public static async Task<PagedResult<InternshipSummaryDto>> Handle(
        ListInternships query, IQuerySession session)
    {
        IQueryable<InternshipSummary> queryable = session.Query<InternshipSummary>();

        if (query.StudentId.HasValue)
            queryable = queryable.Where(s => s.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(s => s.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(s => s.InstitutionId == query.InstitutionId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(s => s.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (!string.IsNullOrWhiteSpace(query.Phase) &&
            InternshipPhase.TryFromName(query.Phase, true, out var internshipPhase))
            queryable = queryable.Where(s => s.PhaseName == internshipPhase.Name);

        if (query.MinAbsenceDays.HasValue)
            queryable = queryable.Where(s => s.TotalAbsenceDays >= query.MinAbsenceDays.Value);

        queryable = queryable.ApplySearch(query.Search, s => s.StudentName);

        // Varsayılan: devamsızlığa göre azalan (en çok devamsız üste)
        var effectiveDescending = string.IsNullOrWhiteSpace(query.SortBy) ? true : query.Descending;
        queryable = queryable.ApplySort(query.SortBy, effectiveDescending,
            defaultSort: s => s.TotalAbsenceDays);

        return await queryable.ToPagedResultAsync(query, s => s.ToDto(s.StudentName, s.BusinessName));
    }
}
