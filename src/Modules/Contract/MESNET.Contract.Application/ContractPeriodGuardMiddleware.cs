using Marten;
using MESNET.Common.Shared;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.ReadModels;

namespace MESNET.Contract.Application;

/// <summary>
/// Kapalı akademik döneme ait sözleşme yazma işlemlerini engeller (#30).
/// IContractPeriodScoped command'larında çalışır:
/// InternshipContractId → InternshipContract.AcademicPeriodId → AcademicPeriodView.IsActive.
/// Her handler'da tekrar etmek yerine tek merkezi Wolverine middleware.
/// </summary>
public static class ContractPeriodGuardMiddleware
{
    public static async Task BeforeAsync(IContractPeriodScoped message, IQuerySession session)
    {
        // Aggregate'i yükle — yoksa handler kendi NOT_FOUND'unu atsın
        var contract = await session.LoadAsync<InternshipContract>(message.InternshipContractId);
        if (contract is null) return;

        var period = await session.LoadAsync<AcademicPeriodView>(contract.AcademicPeriodId);
        if (period is { IsActive: false })
            throw new DomainException(ContractErrors.AcademicPeriodClosed(contract.AcademicPeriodId));
    }
}
