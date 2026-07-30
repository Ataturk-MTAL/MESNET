using MESNET.Common.Shared;

namespace MESNET.Institution.Application.Commands;

public sealed record CreateInstitution(
    int InstitutionCode,
    string FullName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? WebUrl,
    Location? Location,
    // MEB il kodu (01–81) — zorunlu, kapsam anahtarı (#147). Validator boş geçilmesini reddeder.
    string? ProvinceCode = null,
    // İlçe adı — TurkishDistricts kapalı listesinden. İsteğe bağlı.
    string? DistrictName = null,
    Guid? Id = null);
