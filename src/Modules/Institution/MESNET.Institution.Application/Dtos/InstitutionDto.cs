using MESNET.Common.Shared;

namespace MESNET.Institution.Application.Dtos;

public sealed record InstitutionDto(
    Guid Id,
    int InstitutionCode,
    string FullName,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? WebUrl,
    Location? Location,
    // MEB il kodu + okuma anında çözülen adı (#147). Ad DTO'ya konur ki 81 ilin listesi
    // frontend'de ikinci kez tanımlanmasın; kod yetkili, ad görüntü içindir.
    string? ProvinceCode,
    string? ProvinceName,
    string? DistrictName,
    List<InstitutionBranchDto> Branches,
    List<StaffMemberDto> Staff);

/// <summary>Tek il — açılır liste için (kod yetkili, ad görüntü).</summary>
public sealed record ProvinceDto(string Code, string Name);

/// <summary>
/// İl listesi sarmalayıcısı. Wolverine handler'dan dönen <c>IEnumerable&lt;T&gt;</c>'i
/// cascading message sanıp her elemanı yayınlar — koleksiyon somut bir DTO'ya sarılmak zorunda.
/// </summary>
public sealed record ProvinceListDto(List<ProvinceDto> Items);

/// <summary>
/// İlçe adları, alfabetik. Sarmalayıcı, <c>ProvinceListDto</c> ile aynı gerekçeyle:
/// çıplak koleksiyon Wolverine tarafından cascading message sanılır.
/// </summary>
public sealed record DistrictListDto(List<string> Items);

public sealed record InstitutionBranchDto(
    string FieldCode,
    string FieldName,
    string Type,
    string TypeSlug,
    List<string> ActiveSpecializations,
    int AvailableCount,
    int AtWorkCount,
    int TotalCount,
    bool IsActive,
    int DepartmentHeadCount,
    int WorkshopHeadCount);

/// <param name="KeycloakId">
/// Kullanıcının Keycloak kimliği (#190). DTO'da YOKTU; seeder mükerrer kontrolünü bu alandan
/// yapmaya çalışıyor, bulamayınca istisna atıyor ve <c>catch {}</c> onu yutuyordu — her
/// çalıştırmada aynı 5 kişi yeniden ekleniyordu. Kimliğin dışarı verilmesi kaydı yönetmek
/// (mükerrer tespiti, rol değişimi) için gereklidir; uç zaten <c>institution:staff:manage</c>
/// ister.
/// </param>
public sealed record StaffMemberDto(
    Guid Id,
    string KeycloakId,
    string FullName,
    string Role,
    string RoleSlug,
    string? BranchCode,
    DateTime AuthorizedAt);

public sealed record ScheduleConfigDto(
    bool Configured,
    int? DailyPeriodCount,
    DateTime? UpdatedAt,
    // Kimlik saklanır, ad okuma anında UserNameView'dan çözülür (#137).
    Guid? UpdatedById,
    string? UpdatedByName);
