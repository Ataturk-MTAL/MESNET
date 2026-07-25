namespace MESNET.Contract.Shared.Events;

public sealed record ContractCreated(
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    DateTime StartDate,
    // 3308 Madde 25: ücret "düzenlenecek sözleşme ile tespit edilir"; yasal yüzdeler yalnız
    // alt sınırdır. null = sözleşmede belirtilmemiş → yasal taban uygulanır. Eski olaylarda
    // alan yok, deserialize'da null gelir — bilinçli olarak nullable (#84).
    decimal? AgreedMonthlyWage,
    DateTime CreatedAt);
