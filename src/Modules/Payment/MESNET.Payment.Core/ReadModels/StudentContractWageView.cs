namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Öğrencinin yürürlükteki sözleşmesinde taahhüt edilen aylık ücret (#84).
/// Contract modülünün olaylarından beslenir; Payment başka modülün verisini doğrudan sorgulamaz.
///
/// 3308 Madde 25: ücret "düzenlenecek sözleşme ile tespit edilir", kanundaki yüzdeler yalnız
/// alt sınırdır ("aşağı ücret ödenemez"). Sözleşmede daha yüksek ücret varsa esas olan odur.
/// </summary>
public class StudentContractWageView
{
    /// <summary>StudentId — öğrenci başına tek yürürlükteki sözleşme.</summary>
    public Guid Id { get; set; }

    public Guid ContractId { get; set; }
    public Guid BusinessId { get; set; }

    /// <summary>null = sözleşmede ücret belirtilmemiş, yasal taban uygulanır.</summary>
    public decimal? AgreedMonthlyWage { get; set; }

    /// <summary>Sözleşme fesih/tamamlanma ile kapandığında false olur.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime LastUpdated { get; set; }
}
