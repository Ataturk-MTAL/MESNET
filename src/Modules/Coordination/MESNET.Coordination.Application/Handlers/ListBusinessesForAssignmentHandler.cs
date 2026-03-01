using Marten;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class ListBusinessesForAssignmentHandler
{
    public static async Task<List<BusinessAssignmentDto>> Handle(
        ListBusinessesForAssignment query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        IQueryable<BusinessCoordinationView> queryable = session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == query.InstitutionId);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            queryable = queryable.Where(v => v.BranchCode == query.BranchCode);

        if (query.TeacherId.HasValue)
            queryable = queryable.Where(v => v.AssignedTeacherId == query.TeacherId.Value);

        if (query.AssignedOnly == true)
            queryable = queryable.Where(v => v.AssignedTeacherId != null);
        else if (query.AssignedOnly == false)
            queryable = queryable.Where(v => v.AssignedTeacherId == null);

        var views = await queryable.ToListAsync(cancellationToken);

        return views.Select(v => new BusinessAssignmentDto(
            v.Id,
            v.Name,
            v.Address,
            v.District,
            v.DistanceToSchoolKm,
            v.IsManualDistance,
            v.MaxCoordinationHours,
            v.AssignedHours,
            v.AssignedTeacherId,
            v.AssignedTeacherName,
            v.AssignedDay,
            v.ActiveStudentCount,
            v.BranchCode,
            v.BranchName
        )).ToList();
    }
}
