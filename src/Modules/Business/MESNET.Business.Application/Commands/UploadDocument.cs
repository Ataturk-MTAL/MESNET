using MESNET.Business.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace MESNET.Business.Application.Commands;

public sealed record UploadDocument(
    Guid BusinessId,
    DocumentType Type,
    IFormFile DocumentFile,
    DateTime? ExpiresAt);
