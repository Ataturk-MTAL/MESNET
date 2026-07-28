namespace MESNET.Payment.Application.Commands;

/// <remarks>
/// Onaylayan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan
/// (<c>ICurrentUserService.GetUserId()</c>) damgalar. Onay kaydındaki aktörü,
/// onayı yapan istemcinin kendisi yazamaz.
/// </remarks>
public sealed record ApproveReceiptByDeputy(
    Guid SalaryPeriodId) : ISalaryPeriodScoped;
