using Microsoft.AspNetCore.Http;

namespace MESNET.Contract.Application.Commands;

/// <summary>
/// Islak imzalı sözleşme belgesi yükle.
/// </summary>
public sealed record UploadSignedContractDocument(
    Guid ContractId,
    IFormFile DocumentFile,
    string UploadedBy);
