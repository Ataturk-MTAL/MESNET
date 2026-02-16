using Microsoft.AspNetCore.Http;

namespace MESNET.Contract.Application.Commands;

/// <summary>
/// Islak imzalı fesih belgesi yükle.
/// </summary>
public sealed record UploadTerminationDocument(
    Guid ContractId,
    IFormFile DocumentFile,
    string UploadedBy);
