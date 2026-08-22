namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Payment modülünün yerel sözleşme kaydı — Contract olaylarından beslenir. Aylık maaş
/// dönemlerinin çalışma listesi ve gün oranlamasının kaynağı budur (#154).
/// </summary>
/// <remarks>
/// <para><b>Neden yerleştirme değil sözleşme:</b> maaş dönemi anahtarı (sözleşme, ay) oldu.
/// Sözleşme zaten istihdam ilişkisinin kendisidir — öğrenci + işletme + geçerlilik tarihleri.
/// (öğrenci, işletme, ay) yetmezdi: aynı işletmeyle ay içinde yeniden sözleşme yapılırsa gene
/// çakışırdı.</para>
///
/// <para><b>Neyin yerine geçti:</b> <c>StudentContractWageView</c> kaldırıldı. O kayıt
/// <c>StudentId</c> ile anahtarlıydı, yani öğrenci başına TEK sözleşme tutabiliyordu; ay içinde
/// işletme değiştiren öğrencide eski sözleşmenin ücreti kayboluyor ve iki dönem de yeni
/// sözleşmenin ücretiyle hesaplanıyordu. Kayıt artık sözleşme başına.</para>
///
/// <para><b>Neden kapanan sözleşme silinmiyor:</b> ay ortasında feshedilen sözleşme o ayın
/// ücretini hâlâ hak eder. Eski akış kapanan kaydı listeden düşürüyordu ve ayrılınan işletme
/// için maaş dönemi hiç açılmıyordu — öğrenci orada çalıştığı günlerin ücretini alamıyordu.</para>
/// </remarks>
public class ContractEmploymentView
{
    /// <summary>ContractId — maaş dönemi kimliğinin (sözleşme, ay) ilk bileşeni.</summary>
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    /// <summary>Sözleşmenin başlangıç tarihi — istihdam penceresinin alt ucu.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Fesih/tamamlanma tarihi. <c>null</c> = sözleşme hâlâ yürürlükte, ay sonuna kadar sayılır.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Sözleşme aktifleştirildi mi. Taslak (imza bekleyen) sözleşme için maaş dönemi açılmaz;
    /// istihdam henüz başlamamıştır.
    /// </summary>
    public bool IsActivated { get; set; }

    /// <summary>
    /// Sözleşmede taahhüt edilen aylık ücret (#84). <c>null</c> = belirtilmemiş, yasal taban
    /// uygulanır. 3308 Madde 25'teki yüzdeler yalnız alt sınırdır.
    /// </summary>
    public decimal? AgreedMonthlyWage { get; set; }

    public DateTime LastUpdated { get; set; }
}
