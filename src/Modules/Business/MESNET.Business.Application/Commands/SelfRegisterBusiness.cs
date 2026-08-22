using MESNET.Common.Shared;

namespace MESNET.Business.Application.Commands;

public sealed record SelfRegisterBusiness(
    string KeycloakId,
    string FullName,
    string RepresentativePhone,
    string RepresentativeEmail,
    string BusinessName,
    /// <summary>Vergi kimliği — 10 haneli VKN ya da 11 haneli TCKN (#150).</summary>
    string TaxNumber,
    string Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    int PersonnelCount,
    Location? Location,
    int TotalSlots,
    List<string>? Sectors);
