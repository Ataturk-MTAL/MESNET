using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class ListTeachersHandler
{
    public static async Task<PagedResult<TeacherProfileDto>> Handle(ListTeachers query, IQuerySession session)
    {
        IQueryable<TeacherProfile> queryable = session.Query<TeacherProfile>();

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(t => t.InstitutionId == query.InstitutionId.Value);

        if (!string.IsNullOrEmpty(query.BranchCode))
            queryable = queryable.Where(t => t.BranchCode == query.BranchCode);

        queryable = queryable.ApplySearch(query.Search, t => t.FullName);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: t => t.FullName);

        return await queryable.ToPagedResultAsync(query, t => t.ToDto());
    }
}
