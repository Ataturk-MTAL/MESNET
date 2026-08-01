using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Core.Services;

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
    // business-rules.md §6.2: GünlükÜcret = Taban / 30. Aynı sabit oranlamanın da böleni
    // olduğu için tek kaynakta durur (#154) — burada ikinci bir 30 tanımlanmaz.
    private const int DaysInSalaryMonth = EmploymentDays.FullMonthDays;
    private const int ApprenticeshipClassYear = 12; // MESEM 12. sınıf = kalfalık yeterliği
    private const string MesemEducationType = "Mesem";
    private const int MinorAgeThreshold = 16;      // 3308 md.25: "yaşına uygun asgari ücret" (#85)

    public sealed record Result(
        decimal BaseWage,
        decimal Deduction,
        decimal NetAmount,
        decimal GovernmentContribution,
        /// <summary>
        /// Katkının hangi kural gereği hesaplandığı (#157). Önceden oran satır içinde
        /// türetiliyor ve karar hiçbir yere yazılmıyordu; <c>GovernmentContributionType</c>
        /// enum'u tanımlı olmasına rağmen hiç kullanılmıyordu. Kararı döndürmek hem denetimi
        /// hem de "neden bu tutar" sorusunu cevaplanabilir kılar.
        /// </summary>
        GovernmentContributionType ContributionType,
        /// <summary>
        /// Tutarın kaç istihdam günü üzerinden hesaplandığı (#154). Aynı gerekçe:
        /// ay içi fesihte "neden yarım ücret" sorusu kayıttan cevaplanabilmeli.
        /// </summary>
        int EmployedDays);

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
    /// <param name="ageAtCalculation">
    /// Öğrencinin hesap ayındaki yaşı (#85). 3308 Madde 25 ve MEB Ortaöğretim Kurumları
    /// Yönetmeliği (6)(a) "YAŞINA UYGUN asgari ücret" diyor; 16 yaşından küçükler için ayrı
    /// (daha düşük) asgari ücret belirlenir. null (doğum tarihi bilinmiyor) ise genel asgari
    /// ücret uygulanır — eksik veri düşük ödeme üretmesin.
    /// </param>
    /// <param name="isApprentice">
    /// Aday çırak veya çırak mı (#85). Madde 25: "aday çırak ve çırağa yaşına uygun asgari
    /// ücretin yüzde otuzundan ... aşağı ücret ödenemez" — işletme büyüklüğüne bakılmaz.
    /// </param>
    /// <param name="employedDays">
    /// Sözleşmenin o ayda ürettiği istihdam günü (#154) — <see cref="EmploymentDays.InMonth"/>.
    /// Tam ay 30'dur; ay ortası fesih/başlangıçta fiilî gün sayısıdır. Ücret ve devlet katkısı
    /// bu oranla hesaplanır, böylece aynı ayda iki işveren kendi günü kadar yükümlü olur.
    /// Varsayılan tam aydır: oranlamanın gerekmediği çağrı yollarında davranış değişmez.
    /// </param>
    /// <param name="agreedMonthlyWage">
    /// Sözleşmede taahhüt edilen aylık ücret (#84). 3308 Madde 25: ücret "düzenlenecek sözleşme
    /// ile tespit edilir", kanundaki yüzdeler yalnız ALT SINIRDIR ("aşağı ücret ödenemez").
    /// null veya yasal tabandan düşükse yasal taban uygulanır.
    /// </param>
    public static Result Calculate(
        SalaryCalculationConfig config,
        int personnelCount,
        string educationTypeName,
        int classYear,
        bool hasJourneymanQualification,
        int deductibleAbsenceDays,
        decimal? agreedMonthlyWage = null,
        int? ageAtCalculation = null,
        bool isApprentice = false,
        bool isPublicInstitution = false,
        int employedDays = EmploymentDays.FullMonthDays)
    {
        // Savunma: gün sayısı 0–30 aralığının dışına çıkamaz. Bozuk veri negatif ücret ya da
        // tam aydan fazla ödeme üretmemeli.
        employedDays = Math.Clamp(employedDays, 0, EmploymentDays.FullMonthDays);

        var isMesem = string.Equals(educationTypeName, MesemEducationType, StringComparison.OrdinalIgnoreCase);
        var isLargeBusiness = personnelCount >= config.PersonnelThreshold;

        // §6.1 Taban ücret. MESEM %50 oranı yalnız KALFALIK YETERLİĞİNİ KAZANAN 12. sınıf
        // öğrencilerine uygulanır; yeterliği olmayan MESEM öğrencisi işletme büyüklüğü oranına
        // düşer. Yeterlik bilinmiyorsa (varsayılan false) düşük oran uygulanır — eksik veri
        // fazla ödeme üretmesin.
        // Aday çırak/çırak oranı işletme büyüklüğünden bağımsızdır (Madde 25).
        var baseRate = isApprentice
            ? config.ApprenticeRate
            : isMesem && classYear >= ApprenticeshipClassYear && hasJourneymanQualification
                ? config.MEM12thGradeRate
                : isLargeBusiness
                    ? config.LargeBusinessRate
                    : config.SmallBusinessRate;

        // "Yaşına uygun asgari ücret" (#85): 16 yaşından küçüklere ayrı tutar belirlenmişse o
        // uygulanır. Yaş bilinmiyorsa veya ayrı tutar tanımlı değilse genel asgari ücret.
        var applicableMinimumWage = ageAtCalculation is { } age && age < MinorAgeThreshold
            ? config.MinimumWageUnder16 ?? config.MinimumWage
            : config.MinimumWage;

        // Yasal taban — 3308 Madde 25'in altına inilemeyecek tutar.
        var statutoryFloor = applicableMinimumWage * baseRate;

        // Sözleşmede taahhüt edilen ücret varsa ve yasal tabandan yüksekse esas alınan odur (#84).
        // Düşükse sözleşme kanuna aykırıdır; sistem taban ücreti ödemeye devam eder.
        var baseWage = agreedMonthlyWage is { } agreed && agreed > statutoryFloor
            ? agreed
            : statutoryFloor;

        // §6.2 Günlük ücret. Bölen her zaman 30'dur — ayın gün sayısı değil (#154).
        var dailyWage = baseWage / DaysInSalaryMonth;

        // İstihdam günü oranlaması (#154). Ay ortasında fesih edilen ya da başlayan sözleşme
        // yalnız kendi günlerinden sorumludur; aynı ayda iki işveren bölüşür.
        // Tam ayda bölüp çarpma yapılmaz: 10.000/30×30 ondalık artıkla tam 10.000 etmez.
        var proratedWage = employedDays >= DaysInSalaryMonth
            ? baseWage
            : dailyWage * employedDays;

        // §6.2 Devamsızlık kesintisi — mazeretsiz devamsızlık ve ücretsiz izin günleri.
        // Kesinti günlük ücrete bağlıdır; tavanı ORANLANMIŞ tutardır (tam ay tabanı değil),
        // yoksa yarım ay çalışan öğrencinin ücreti negatife düşebilirdi.
        var deduction = dailyWage * deductibleAbsenceDays;
        if (deduction > proratedWage) deduction = proratedWage;   // ücret negatife düşemez

        var netAmount = proratedWage - deduction;

        // §6.3 Devlet katkısı. Geçici Madde 12 matrahı "ÖDENEBİLECEK EN AZ ÜCRET" olarak
        // tanımlıyor — yani Madde 25'teki yasal taban. Önceden kesinti düşülmüş net üzerinden
        // hesaplanıyordu ve devlet katkısı eksik çıkıyordu (#83).
        // MESEM'de sınıf/yeterlik şartı YOK: "mesleki eğitim merkezi programına devam eden
        // öğrencilere ödenebilecek en az ücretin ise tamamı".
        // Kamu kurumlarına devlet katkısı ÖDENMEZ (#157) — 3308 Geçici Madde 12:
        // "Kamu kurum ve kuruluşlarına Devlet katkısı ödenmez."
        //
        // Kontrol diğer tüm oranlardan ÖNCE gelir: kamu kurumu için MESEM/işletme büyüklüğü
        // ayrımının hiçbir anlamı yok, katkı sıfırdır. Bu kural bugüne kadar hiç
        // uygulanmıyordu; GovernmentContributionType.PublicInstitution enum değeri tanımlıydı
        // ama hiçbir kod onu atamıyor ya da kontrol etmiyordu.
        if (isPublicInstitution)
        {
            return new Result(
                proratedWage, deduction, netAmount,
                GovernmentContribution: 0m,
                ContributionType: GovernmentContributionType.PublicInstitution,
                EmployedDays: employedDays);
        }

        var contributionType = isMesem
            ? GovernmentContributionType.MemStudent
            : isLargeBusiness
                ? GovernmentContributionType.NonMemLarge
                : GovernmentContributionType.NonMemSmall;

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
        // Matrah YASAL TABAN, sözleşmedeki fazlası değil (#84): Geçici Madde 12 katkıyı
        // "ödenebilecek EN AZ ücret" üzerinden tanımlıyor. İşletme daha yüksek ücret ödemeyi
        // seçtiyse aradaki fark işveren payına eklenir, devlet katkısını büyütmez.
        //
        // Matrah da istihdam günüyle oranlanır (#154): işletme öğrenciyi 15 gün çalıştırdıysa
        // teşvikin de yarısını alır. Kural sahibi: "hangi işletmede kaç gün çalıştıysa o oranda
        // işletme katkı alır." Net tavanı oranlamadan SONRA uygulanır, yoksa yarım ayda katkı
        // fiilen ödenen ücreti aşabilirdi.
        var proratedFloor = employedDays >= DaysInSalaryMonth
            ? statutoryFloor
            : statutoryFloor / DaysInSalaryMonth * employedDays;

        var govContribution = Math.Min(proratedFloor * govRate, netAmount);

        return new Result(
            proratedWage, deduction, netAmount, govContribution, contributionType, employedDays);
    }
}
