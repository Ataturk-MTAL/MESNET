using Microsoft.AspNetCore.Http;

namespace MESNET.Business.Application.Commands;

/// <summary>
/// Usta öğretici belgesi yükle (işletme tarafından).
/// </summary>
public sealed record UploadInstructorDocument(
    Guid BusinessId,
    IFormFile DocumentFile,
    string UploadedBy,
    DateTime? ExpiresAt);
