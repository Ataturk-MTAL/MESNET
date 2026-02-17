using MESNET.Reporting.Core.Enums;

namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Liste sorguları için FormDataJson hariç projeksiyon DTO'su
/// </summary>
public sealed class GeneratedDocumentSummary
{
    public Guid Id { get; init; }
    public required MebFormType FormType { get; init; }
    public required PhysicalDocumentStatus Status { get; init; }

    public Guid? StudentId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    public required string GeneratedByName { get; init; }
    public DateTime GeneratedAt { get; init; }

    public DateTime? PrintedAt { get; init; }
    public string? PrintedByName { get; init; }

    public DateTime? SignedAndReturnedAt { get; init; }
    public string? ReturnedByName { get; init; }

    public DateTime? ArchivedAt { get; init; }
    public string? ArchivedByName { get; init; }

    public string? AcademicYear { get; init; }
    public string? Description { get; init; }
}
