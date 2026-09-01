namespace MESNET.Audit.Application.Dtos;

/// <param name="OutcomeSlug">Türkçe rozet metni; arayüz kendi eşleme tablosunu tutmaz.</param>
public sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string ActorName,
    string CommandType,
    string CommandLabel,
    string Module,
    Guid? SubjectInstitutionId,
    bool CrossedTenantBoundary,
    string Outcome,
    string OutcomeSlug,
    string? ErrorCode,
    IReadOnlyDictionary<string, Guid> TargetIds,
    int DurationMs);
