namespace MESNET.Coordination.Application.Commands;

/// <param name="IsHonoraryVisit">
/// Fahri (ücretsiz) ziyaret işareti (#115). True ise <paramref name="AssignedHours"/>
/// 0 kabul edilir; havuz ve öğretmen kapasitesi kısıtları uygulanmaz.
/// </param>
/// <remarks>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar.
/// </remarks>
public sealed record UpdateBusinessAssignedHours(
    Guid BusinessId,
    int AssignedHours,
    Guid InstitutionId,
    string BranchCode = "",
    Guid AcademicPeriodId = default,
    bool IsHonoraryVisit = false);
