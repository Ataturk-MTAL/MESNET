using MESNET.Common.Shared;

namespace MESNET.Business.Application.Commands;

public sealed record UpdateBusinessInfo(
    Guid BusinessId,
    string? Name,
    /// <summary>Vergi kimliği düzeltmesi (#150). <c>null</c> = dokunma.</summary>
    string? TaxNumber,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    int? PersonnelCount,
    Location? Location,
    List<string>? Sectors,
    /// <summary>
    /// Kamu/özel ayrımı düzeltmesi (#157). <c>null</c> = dokunma — kısmi güncelleme deseni,
    /// diğer alanlarla aynı.
    /// </summary>
    bool? IsPublicInstitution = null);
