namespace MESNET.Contract.Application.Commands;

/// <summary>
/// İşletme yöneticisi fesih talebinde bulunur.
/// Kurum yönetimi tarafından onay/red beklenir.
/// </summary>
public sealed record RequestTermination(
    Guid InternshipContractId,
    string Reason,
    string ReasonType,
    string RequestedBy) : IContractPeriodScoped;
