namespace MESNET.Institution.Application.Dtos;

public sealed record FieldOfStudyDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string TypeSlug,
    List<SpecializationDto> Specializations,
    bool IsProtocolBased,
    bool IsActive);

public sealed record SpecializationDto(
    string Code,
    string Name,
    bool IsActive);
