using MESNET.Business.Core.Enums;

namespace MESNET.Business.Application.Commands;

public sealed record UploadDocument(
    Guid BusinessId,
    DocumentType Type,
    string FileName,
    string? StoragePath,
    DateTime? ExpiresAt);
