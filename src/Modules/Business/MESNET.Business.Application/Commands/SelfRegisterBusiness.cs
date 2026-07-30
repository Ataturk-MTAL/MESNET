using MESNET.Common.Shared;

namespace MESNET.Business.Application.Commands;

public sealed record SelfRegisterBusiness(
    string KeycloakId,
    string FullName,
    string RepresentativePhone,
    string RepresentativeEmail,
    string BusinessName,
    string Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    int PersonnelCount,
    Location? Location,
    int TotalSlots,
    List<string>? Sectors);
