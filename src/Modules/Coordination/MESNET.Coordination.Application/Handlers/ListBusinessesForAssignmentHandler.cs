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

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(v => v.AcademicPeriodId == query.AcademicPeriodId.Value);

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
            v.AssignedPeriodNumber,
            v.ActiveStudentCount,
            v.BranchCode,
            v.BranchName,
            v.AssignedSlots.Select(s => new AssignedSlotInfoDto(s.Day, s.PeriodNumber)).ToList(),
            v.LastModifiedAt,
            v.LastModifiedBy
        )).ToList();
    }
}
