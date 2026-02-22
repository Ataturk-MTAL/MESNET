using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class ListTeachersHandler
{
    public static async Task<IReadOnlyList<TeacherProfileDto>> Handle(ListTeachers query, IQuerySession session)
    {
        IQueryable<TeacherProfile> queryable = session.Query<TeacherProfile>();

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(t => t.InstitutionId == query.InstitutionId.Value);

        // TeacherProfile dönem bağımsız — filtre uygulanmaz

        var teachers = await queryable.ToListAsync();
        return teachers.Select(t => t.ToDto()).ToList();
    }
}
