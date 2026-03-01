using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Commands;

public sealed record UpsertCoordinationConfig(
    Guid InstitutionId,
    List<DistanceHourRule>? DistanceHourRules,
    bool? IsMetropolitan,
    int? MaxWeeklyExtraHours,
    string UpdatedBy);
