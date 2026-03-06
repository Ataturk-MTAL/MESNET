namespace MESNET.Institution.Shared.Events;

/// <summary>
/// Alan şefliği ve atölye şefliği yapılandırması güncellendiğinde yayınlanır.
/// Coordination modülü bu event'i dinleyerek BranchWorkloadConfig'i günceller.
/// </summary>
public sealed record BranchSupervisorConfigUpdated(
    Guid InstitutionId,
    string FieldCode,
    int DepartmentHeadCount,
    int WorkshopHeadCount);
