using Microsoft.AspNetCore.Http;

namespace MESNET.Payment.Application.Commands;

public sealed record UploadReceiptByBusiness(
    Guid SalaryPeriodId,
    Guid StudentId,
    Guid BusinessId,
    int Month,
    int Year,
    IFormFile ReceiptFile) : ISalaryPeriodScoped;
