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
    /// <param name="personnelCount">İşletmenin personel sayısı.</param>
    /// <param name="educationTypeName"><c>Formal</c> veya <c>Mesem</c>.</param>
    /// <param name="classYear">Öğrencinin sınıfı.</param>
    /// <param name="unexcusedDays">Ay içindeki onaylanmış mazeretsiz devamsızlık gün sayısı.</param>
    public static Result Calculate(
        SalaryCalculationConfig config,
        int personnelCount,
        string educationTypeName,
        int classYear,
        int unexcusedDays)
    {
        var isMesem = string.Equals(educationTypeName, MesemEducationType, StringComparison.OrdinalIgnoreCase);
        var isLargeBusiness = personnelCount >= config.PersonnelThreshold;

        // §6.1 Taban ücret — MESEM 12. sınıf oranı işletme büyüklüğünün önüne geçer.
        var baseRate = isMesem && classYear >= ApprenticeshipClassYear
            ? config.MEM12thGradeRate
            : isLargeBusiness
                ? config.LargeBusinessRate
                : config.SmallBusinessRate;

        var baseWage = config.MinimumWage * baseRate;

        // §6.2 Devamsızlık kesintisi — yalnız mazeretsiz günler (AbsenceType.AffectsSalary).
        var dailyWage = baseWage / DaysInSalaryMonth;
        var deduction = dailyWage * unexcusedDays;
        if (deduction > baseWage) deduction = baseWage;   // ücret negatife düşemez

        var netAmount = baseWage - deduction;

        // §6.3 Devlet katkısı — MESEM'de sınıf şartı YOK, tüm MESEM öğrencilerinde ücretin tamamı.
        var govRate = isMesem
            ? config.GovContribMEM
            : isLargeBusiness
                ? config.GovContribLargeNonMEM
                : config.GovContribSmallNonMEM;

        return new Result(baseWage, deduction, netAmount, netAmount * govRate);
    }
}
