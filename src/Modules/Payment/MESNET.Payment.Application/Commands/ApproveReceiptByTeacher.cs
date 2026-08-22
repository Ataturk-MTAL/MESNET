namespace MESNET.Payment.Application.Commands;

/// <remarks>
/// Onaylayan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan
/// (<c>ICurrentUserService.GetUserId()</c>) damgalar.
/// </remarks>
public sealed record ApproveReceiptByTeacher(
    Guid SalaryPeriodId) : ISalaryPeriodScoped;
