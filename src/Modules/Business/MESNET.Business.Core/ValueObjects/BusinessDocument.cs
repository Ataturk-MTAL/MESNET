using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Business.Core.Enums;

namespace MESNET.Business.Core.ValueObjects;

public sealed record BusinessDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [JsonConverter(typeof(SmartEnumNameConverter<DocumentType, int>))]
    public required DocumentType Type { get; init; }

    [JsonConverter(typeof(SmartEnumNameConverter<DocumentStatus, int>))]
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public required string FileName { get; init; }
    public string? StoragePath { get; init; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; init; }
    public string? RejectionReason { get; set; }
}
