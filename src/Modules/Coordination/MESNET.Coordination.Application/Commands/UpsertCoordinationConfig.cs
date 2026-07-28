using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan
/// (<c>ICurrentUserService.GetUserId()</c>) damgalar. Aksi hâlde denetim satırındaki
/// aktörü, işlemi yapan istemcinin kendisi yazardı.
/// </summary>
public sealed record UpsertCoordinationConfig(
    Guid InstitutionId,
    List<DistanceHourRule>? DistanceHourRules,
    bool? IsMetropolitan,
    int? MaxWeeklyExtraHours);
