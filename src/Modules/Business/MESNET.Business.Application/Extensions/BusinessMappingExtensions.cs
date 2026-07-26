using MESNET.Business.Application.Dtos;
using MESNET.Business.Core.Enums;

namespace MESNET.Business.Application.Extensions;

public static class BusinessMappingExtensions
{
    public static BusinessDto ToDto(this Core.Entities.Business entity) => new(
        entity.Id,
        entity.Name,
        entity.Address,
        entity.PhoneNumber,
        entity.Email,
        entity.Website,
        entity.Status.Name,
        entity.Status.Slug,
        entity.Source.Name,
        entity.Source.Slug,
        entity.PersonnelCount,
        entity.Location,
        entity.Capacity.ToDto(),
        entity.Representatives.Select(r => r.ToDto()).ToList(),
        entity.Documents.Select(d => d.ToDto()).ToList(),
        entity.Sectors.Select(s => s.ToSectorDto()).ToList(),
        entity.CreatedAt,
        entity.ApprovedAt,
        entity.ClosedAt);

    /// <summary>
    /// Olaylarla dışa taşınan "İşletme Yetkilisi" adı — ilk yetkilendirilmiş temsilci (#99).
    /// Temsilci yoksa null döner; tüketen modüller alanı opsiyonel olarak ele alır.
    /// </summary>
    public static string? PrimaryRepresentativeName(this Core.Entities.Business entity) =>
        entity.Representatives
            .OrderBy(r => r.AuthorizedAt)
            .FirstOrDefault()?.FullName;

    public static SectorDto ToSectorDto(this string sectorName) =>
        BusinessSector.TryFromName(sectorName, true, out var sector)
            ? new SectorDto(sector.Name, sector.Slug)
            : new SectorDto(sectorName, sectorName);

    public static BusinessCapacityDto ToDto(this Core.ValueObjects.BusinessCapacity vo) => new(
        vo.TotalSlots,
        vo.OccupiedSlots,
        vo.AvailableSlots,
        vo.IsFull);

    public static BusinessRepresentativeDto ToDto(this Core.ValueObjects.BusinessRepresentative vo) => new(
        vo.Id,
        vo.FullName,
        vo.PhoneNumber,
        vo.Email,
        vo.AuthorizedAt);

    public static BusinessDocumentDto ToDto(this Core.ValueObjects.BusinessDocument vo) => new(
        vo.Id,
        vo.Type.Name,
        vo.Type.Slug,
        vo.Status.Name,
        vo.Status.Slug,
        vo.FileName,
        vo.UploadedAt,
        vo.ApprovedAt,
        vo.ExpiresAt,
        vo.RejectionReason);
}
