using Microsoft.AspNetCore.Http;

namespace MESNET.Contract.Application.Commands;

/// <summary>
/// Sözleşmeye evrak yükle. DocumentType: SignedContract | TerminationLetter | Other
/// </summary>
public sealed record UploadContractDocument(
    Guid ContractId,
    IFormFile DocumentFile,
    string DocumentType,
    string? Description,
    string UploadedBy);
