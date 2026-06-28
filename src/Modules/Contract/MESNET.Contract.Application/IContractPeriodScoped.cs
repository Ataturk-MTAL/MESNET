namespace MESNET.Contract.Application;

/// <summary>
/// Belirli bir staj sözleşmesine (InternshipContractId) bağlı yazma command'ları için marker.
/// ContractPeriodGuardMiddleware bu command'larda kapalı akademik dönem kontrolü yapar.
/// </summary>
public interface IContractPeriodScoped
{
    Guid InternshipContractId { get; }
}
