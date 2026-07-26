namespace MESNET.Coordination.Application.Commands;

/// <param name="IsHonoraryVisit">
/// Fahri (ücretsiz) ziyaret işareti (#115). True ise <paramref name="AssignedHours"/>
/// 0 kabul edilir; havuz ve öğretmen kapasitesi kısıtları uygulanmaz.
/// </param>
public sealed record UpdateBusinessAssignedHours(
    Guid BusinessId,
    int AssignedHours,
    Guid InstitutionId,
    string UpdatedBy,
    string BranchCode = "",
    Guid AcademicPeriodId = default,
    bool IsHonoraryVisit = false);
