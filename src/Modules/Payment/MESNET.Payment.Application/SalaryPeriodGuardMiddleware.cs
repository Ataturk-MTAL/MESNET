using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application;

/// <summary>
/// Kapalı akademik döneme ait maaş/dekont yazma işlemlerini engeller (#8).
/// ISalaryPeriodScoped command'larında çalışır:
/// SalaryPeriodId → PaymentSummary → AcademicPeriodId → AcademicPeriodView.IsActive.
/// Her handler'da tekrar etmek yerine tek merkezi Wolverine middleware.
/// </summary>
public static class SalaryPeriodGuardMiddleware
{
    public static async Task BeforeAsync(ISalaryPeriodScoped message, IQuerySession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(message.SalaryPeriodId);
        if (summary is null) return;

        var period = await session.LoadAsync<AcademicPeriodView>(summary.AcademicPeriodId);
        if (period is { IsActive: false })
            throw new DomainException(PaymentErrors.AcademicPeriodClosed(summary.AcademicPeriodId));
    }
}
