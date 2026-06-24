using Microsoft.AspNetCore.Http;

namespace MESNET.Payment.Application.Commands;

public sealed record UploadReceiptByStudent(
    Guid SalaryPeriodId,
    Guid StudentId,
    int Month,
    int Year,
    IFormFile ReceiptFile) : ISalaryPeriodScoped;
