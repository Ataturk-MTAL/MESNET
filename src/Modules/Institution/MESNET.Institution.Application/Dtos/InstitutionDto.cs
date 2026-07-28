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
    List<InstitutionBranchDto> Branches,
    List<StaffMemberDto> Staff);

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

public sealed record StaffMemberDto(
    Guid Id,
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
