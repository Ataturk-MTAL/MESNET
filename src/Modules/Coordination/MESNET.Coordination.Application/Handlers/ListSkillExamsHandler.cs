using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class ListSkillExamsHandler
{
    public static async Task<PagedResult<SkillExam>> Handle(
        ListSkillExams query, IQuerySession session)
    {
        IQueryable<SkillExam> queryable = session.Query<SkillExam>();

        if (query.StudentId.HasValue)
            queryable = queryable.Where(e => e.StudentId == query.StudentId.Value);
        if (query.BusinessId.HasValue)
            queryable = queryable.Where(e => e.BusinessId == query.BusinessId.Value);
        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(e => e.AcademicPeriodId == query.AcademicPeriodId.Value);
        if (query.AcademicYear.HasValue)
            queryable = queryable.Where(e => e.AcademicYear == query.AcademicYear.Value);

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: e => e.ExamDate);

        return await queryable.ToPagedResultAsync(query);
    }
}
