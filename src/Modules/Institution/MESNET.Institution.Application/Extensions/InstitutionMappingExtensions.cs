using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.ValueObjects;

namespace MESNET.Institution.Application.Extensions;

public static class InstitutionMappingExtensions
{
    public static InstitutionDto ToDto(this Core.Entities.Institution entity) => new(
        entity.Id,
        entity.InstitutionCode,
        entity.FullName,
        entity.Address,
        entity.PhoneNumber,
        entity.Email,
        entity.WebUrl,
        entity.Location,
        entity.ProvinceCode,
        TurkishProvinces.GetName(entity.ProvinceCode),
        entity.DistrictName,
        entity.Branches.Select(b => b.ToDto()).ToList(),
        entity.Staff.Select(s => s.ToDto()).ToList());

    public static InstitutionBranchDto ToDto(this InstitutionBranch vo) => new(
        vo.FieldCode,
        vo.FieldName,
        vo.Type.Name,
        vo.Type.Slug,
        vo.ActiveSpecializations,
        vo.AvailableCount,
        vo.AtWorkCount,
        vo.TotalCount,
        vo.IsActive,
        vo.DepartmentHeadCount,
        vo.WorkshopHeadCount);

    public static StaffMemberDto ToDto(this StaffMember vo) => new(
        vo.Id,
        vo.KeycloakId,
        vo.FullName,
        vo.Role.Name,
        vo.Role.Slug,
        vo.BranchCode,
        vo.AuthorizedAt);

    public static FieldOfStudyDto ToDto(this FieldOfStudy entity) => new(
        entity.Id,
        entity.Code,
        entity.Name,
        entity.Type.Name,
        entity.Type.Slug,
        entity.Specializations.Select(s => s.ToDto()).ToList(),
        entity.IsProtocolBased,
        entity.IsActive);

    public static SpecializationDto ToDto(this Specialization vo) => new(
        vo.Code,
        vo.Name,
        vo.IsActive);
}
