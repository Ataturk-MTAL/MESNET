using MESNET.Payment.Core.Entities;

namespace MESNET.Payment.Application.Services;

/// <summary>
/// 3308 sayılı Kanun Madde 25 maaş hesabı. Saf fonksiyon — G/Ç yapmaz.
/// </summary>
/// <remarks>
/// Kurallar <c>src/Docs/docs/architecture/business-rules.md</c> §6'da tanımlı. Oranların tamamı
/// <see cref="SalaryCalculationConfig"/>'den okunur; kodda sabit oran yoktur.
///
/// Önceki hâlinde taban ücret ham asgari ücret olarak alınıyordu (öğrenciye %15/%30/%50 yerine
/// tamamı yazılıyordu, 2–6,7 kat fazla), kesinti hep 0'dı ve devlet katkısı sabit 0.3333 idi (#64).
/// </remarks>
public static class SalaryCalculator
{
    private const int DaysInSalaryMonth = 30;      // business-rules.md §6.2: GünlükÜcret = Taban / 30
    private const int ApprenticeshipClassYear = 12; // MESEM 12. sınıf = kalfalık yeterliği
    private const string MesemEducationType = "Mesem";

    public sealed record Result(
        decimal BaseWage,
        decimal Deduction,
        decimal NetAmount,
        decimal GovernmentContribution);

    /// <param name="config">Kurumun yürürlükteki maaş parametreleri.</param>
    /// <param name="personnelCount">
    /// İşletmenin personel sayısı. 3308 Madde 24 son fıkra: "görev ve çalışma statüsüne
    /// bakılmaksızın işyerinde 1475 sayılı İş Kanununa tabi olarak çalıştırılan personel sayısı".
    /// Stajyer ve çıraklar bu sayıya dâhil değildir (4857 Madde 4/f çırakları kapsam dışı bırakır).
    /// </param>
    /// <param name="educationTypeName"><c>Formal</c> veya <c>Mesem</c>.</param>
    /// <param name="classYear">Öğrencinin sınıfı.</param>
    /// <param name="hasJourneymanQualification">
    /// Kalfalık yeterliğini kazandı mı. %50 oranı yalnız "kalfalık yeterliğini kazanan mesleki
    /// eğitim merkezi 12'nci sınıf öğrencileri" için geçerlidir (#83).
    /// </param>
    /// <param name="deductibleAbsenceDays">
    /// Ay içindeki onaylanmış, ücret kesintisine tabi devamsızlık gün sayısı — mazeretsiz
    /// devamsızlık ve ücretsiz izin günleri (<c>AbsenceType.AffectsSalary</c>).
    /// </param>
    public static Result Calculate(
        SalaryCalculationConfig config,
        int personnelCount,
        string educationTypeName,
        int classYear,
        bool hasJourneymanQualification,
        int deductibleAbsenceDays)
    {
        var isMesem = string.Equals(educationTypeName, MesemEducationType, StringComparison.OrdinalIgnoreCase);
        var isLargeBusiness = personnelCount >= config.PersonnelThreshold;

        // §6.1 Taban ücret. MESEM %50 oranı yalnız KALFALIK YETERLİĞİNİ KAZANAN 12. sınıf
        // öğrencilerine uygulanır; yeterliği olmayan MESEM öğrencisi işletme büyüklüğü oranına
        // düşer. Yeterlik bilinmiyorsa (varsayılan false) düşük oran uygulanır — eksik veri
        // fazla ödeme üretmesin.
        var baseRate = isMesem && classYear >= ApprenticeshipClassYear && hasJourneymanQualification
            ? config.MEM12thGradeRate
            : isLargeBusiness
                ? config.LargeBusinessRate
                : config.SmallBusinessRate;

        var baseWage = config.MinimumWage * baseRate;

        // §6.2 Devamsızlık kesintisi — mazeretsiz devamsızlık ve ücretsiz izin günleri.
        var dailyWage = baseWage / DaysInSalaryMonth;
        var deduction = dailyWage * deductibleAbsenceDays;
        if (deduction > baseWage) deduction = baseWage;   // ücret negatife düşemez

        var netAmount = baseWage - deduction;

        // §6.3 Devlet katkısı. Geçici Madde 12 matrahı "ÖDENEBİLECEK EN AZ ÜCRET" olarak
        // tanımlıyor — yani Madde 25'teki yasal taban. Önceden kesinti düşülmüş net üzerinden
        // hesaplanıyordu ve devlet katkısı eksik çıkıyordu (#83).
        // MESEM'de sınıf/yeterlik şartı YOK: "mesleki eğitim merkezi programına devam eden
        // öğrencilere ödenebilecek en az ücretin ise tamamı".
        var govRate = isMesem
            ? config.GovContribMEM
            : isLargeBusiness
                ? config.GovContribLargeNonMEM
                : config.GovContribSmallNonMEM;

        // Katkı fiilen ödenen ücreti aşamaz: aşarsa işveren payı (Net - Katkı) negatife düşer,
        // yani devlet öğrencinin aldığından fazlasını karşılamış olur. Kesintinin katkı matrahını
        // düşürüp düşürmediği kanunda yazmıyor (Geçici Madde 12: "usul ve esaslar Bakanlık ve
        // Türkiye İş Kurumu tarafından belirlenir") — bu tavan, ikincil mevzuat netleşene kadar
        // güvenli sınır. Normal devamsızlık seviyelerinde devreye girmez.
        var govContribution = Math.Min(baseWage * govRate, netAmount);

        return new Result(baseWage, deduction, netAmount, govContribution);
    }
}
