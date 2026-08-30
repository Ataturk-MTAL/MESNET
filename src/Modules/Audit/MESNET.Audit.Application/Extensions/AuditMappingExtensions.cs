using MESNET.Audit.Application.Dtos;
using MESNET.Audit.Core.Entities;

namespace MESNET.Audit.Application.Extensions;

public static class AuditMappingExtensions
{
    /// <remarks>
    /// <b>Kiracı kimliği ve yol DTO'ya ÇIKMAZ.</b> İkisi de kapsam kararının iç girdisidir;
    /// dışarı verilmeleri, kapsamı olmayan bir okuyucuya ağacın şeklini sızdırırdı.
    /// </remarks>
    public static AuditEntryDto ToDto(this AuditEntry entry) => new(
        entry.Id,
        entry.OccurredAt,
        entry.ActorId,
        entry.ActorName,
        entry.CommandType,
        entry.CommandLabel,
        entry.Module,
        entry.SubjectInstitutionId,
        entry.CrossedTenantBoundary,
        entry.Outcome.Name,
        entry.Outcome.Slug,
        entry.ErrorCode,
        entry.TargetIds,
        entry.DurationMs);
}
