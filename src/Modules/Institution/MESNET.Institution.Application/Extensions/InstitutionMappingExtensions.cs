using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ValueObjects;

namespace MESNET.Institution.Application.Extensions;

public static class InstitutionMappingExtensions
{
    /// <param name="parentName">
    /// Üst düğümün adı. Entity onu bilmez (yalnız <c>ParentId</c> tutar) ve bu uzantı saf
    /// kalmalıdır — bir session açıp okumaz. Sorgu tarafı üst düğümleri <b>toplu</b> okur
    /// (<c>LoadManyAsync</c>) ve buraya geçirir; aksi hâlde her satır için bir okuma olurdu.
    /// </param>
    public static InstitutionDto ToDto(this Core.Entities.Institution entity, string? parentName = null)
    {
        // Saklanan anahtar burada palete çözülür. Null (hiç seçim yapılmamış) ve tanınmayan
        // değer aynı yere, varsayılana düşer — arayüz her zaman geçerli bir tema alır.
        var palette = InstitutionBrandPalette.Resolve(entity.BrandPaletteName);

        // Aynı disiplin düğüm tipinde de geçerli: null (geçiş koşmamış eski kayıt) ve tanınmayan
        // değer en dar okumaya, School'a düşer.
        var nodeType = entity.NodeType;

        return new InstitutionDto(
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
            nodeType.Name,
            nodeType.Slug,
            entity.ParentId,
            parentName,
            palette.Name,
            palette.Slug,
            palette.Primary,
            palette.Secondary,
            entity.Branches.Select(b => b.ToDto()).ToList(),
            entity.Staff.Select(s => s.ToDto()).ToList());
    }

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
