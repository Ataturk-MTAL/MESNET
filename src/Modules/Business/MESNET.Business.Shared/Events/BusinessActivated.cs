using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

/// <param name="RegisteredByInstitutionId">
/// İşletmeyi <b>kaydeden</b> okul — provenance, kapsam DEĞİL (ADR-0003 adım 4).
/// </param>
public sealed record BusinessActivated(
    Guid BusinessId,
    Guid RegisteredByInstitutionId,
    string Name,
    string? Address,
    Location? Location);
