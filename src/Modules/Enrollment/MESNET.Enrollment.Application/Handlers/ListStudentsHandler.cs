using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;

namespace MESNET.Enrollment.Application.Handlers;

public static class ListStudentsHandler
{
    public static async Task<IReadOnlyList<StudentProfileDto>> Handle(ListStudents query, IQuerySession session)
    {
        IQueryable<StudentProfile> queryable = session.Query<StudentProfile>();

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(s => s.InstitutionId == query.InstitutionId.Value);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            queryable = queryable.Where(s => s.BranchCode == query.BranchCode);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            StudentStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(s => s.Status == status);

        var students = await queryable.ToListAsync();
        return students.Select(s => s.ToDto()).ToList();
    }
}
