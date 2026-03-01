using Marten;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class GetTeacherWorkloadHandler
{
    public static async Task<TeacherWorkloadDto> Handle(
        GetTeacherWorkload query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var views = await session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == query.InstitutionId
                        && v.AssignedTeacherId == query.TeacherId)
            .ToListAsync(cancellationToken);

        var businesses = views.Select(v => new TeacherBusinessAssignmentDto(
            v.Id,
            v.Name,
            v.AssignedHours,
            v.AssignedDay
        )).ToList();

        return new TeacherWorkloadDto(
            query.TeacherId,
            views.Sum(v => v.AssignedHours),
            views.Count,
            businesses
        );
    }
}
