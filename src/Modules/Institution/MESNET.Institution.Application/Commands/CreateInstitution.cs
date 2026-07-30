using MESNET.Common.Shared;

namespace MESNET.Institution.Application.Commands;

public sealed record CreateInstitution(
    Guid TenantId,
    int InstitutionCode,
    string FullName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? WebUrl,
    Location? Location,
    // MEB il kodu (01–81) — zorunlu, kapsam anahtarı (#147). Validator boş geçilmesini reddeder.
    string? ProvinceCode = null,
    // MEB ilçe kodu — isteğe bağlı; ilçe kapsamı henüz karara bağlanmadı (#147).
    string? DistrictCode = null,
    Guid? Id = null);
