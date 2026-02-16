using MESNET.Business.Application.Dtos;

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
        entity.CreatedAt,
        entity.ApprovedAt,
        entity.ClosedAt);

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
