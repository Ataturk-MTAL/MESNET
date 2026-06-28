namespace MESNET.Payment.Application;

/// <summary>
/// Belirli bir maaş dönemine (SalaryPeriodId) bağlı yazma command'ları için marker.
/// SalaryPeriodGuardMiddleware bu command'larda kapalı akademik dönem kontrolü yapar.
/// </summary>
public interface ISalaryPeriodScoped
{
    Guid SalaryPeriodId { get; }
}
