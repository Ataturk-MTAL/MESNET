using MESNET.Common.Shared;

using MESNET.Institution.Application.Security;
namespace MESNET.Institution.Application.Commands;

public sealed record UpdateInstitution(
    Guid InstitutionId,
    string? FullName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? WebUrl,
    Location? Location,
    // MEB il kodu (01–81) — null = değiştirme.
    string? ProvinceCode = null,
    // İlçe adı (TurkishDistricts) — null = değiştirme.
    string? DistrictName = null,
    // MEB kurum kodu — null = değiştirme. Kayıtta girilir, sonradan düzeltilebilir.
    int? InstitutionCode = null) : IInstitutionScoped;
