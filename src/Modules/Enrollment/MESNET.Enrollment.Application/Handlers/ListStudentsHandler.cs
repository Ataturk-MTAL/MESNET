using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;

namespace MESNET.Enrollment.Application.Handlers;

public static class ListStudentsHandler
{
    public static async Task<PagedResult<StudentProfileDto>> Handle(ListStudents query, IQuerySession session)
    {
        IQueryable<StudentProfile> queryable = session.Query<StudentProfile>();

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(s => s.InstitutionId == query.InstitutionId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(s => s.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            queryable = queryable.Where(s => s.BranchCode == query.BranchCode);

        if (!string.IsNullOrWhiteSpace(query.Section))
            queryable = queryable.Where(s => s.Section == query.Section);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            StudentStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(s => s.StatusName == status.Name);

        queryable = queryable.ApplySearch(query.Search, s => s.FullName, s => s.TcKimlikNo);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: s => s.FullName);

        return await queryable.ToPagedResultAsync(query, s => s.ToDto());
    }
}
