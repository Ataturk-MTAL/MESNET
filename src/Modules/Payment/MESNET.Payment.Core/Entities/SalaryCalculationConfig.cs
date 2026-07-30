namespace MESNET.Payment.Core.Entities;

/// <summary>
/// Maaş ve devlet katkısı hesabının parametreleri — <b>ULUSAL</b> kapsamlıdır (#147).
/// </summary>
/// <remarks>
/// <para>Buradaki alanların tamamı mevzuattan gelir: asgari ücret (Asgari Ücret Tespit Komisyonu,
/// Resmî Gazete), 3308 Madde 25 taban oranı, Geçici Madde 12 devlet katkısı kesirleri. Kurum
/// düzeyinde belirlenen tek bir alan YOKTUR.</para>
///
/// <para><b>Neden kurum kapsamı kaldırıldı:</b> <c>InstitutionId</c> ile tutulduğunda her okul
/// aynı ulusal sayıyı ayrı ayrı giriyordu ve değerler sapabiliyordu — bir okul
/// <c>ApprenticeRate</c>'i <c>0.35m</c> yazsa parayı yanlış öder, üstelik kendi kapsamı içinde
/// meşru bir yazma olduğu için hiçbir yerde görünmezdi. Ayrıca yazma ucu kurum kimliğini
/// istek gövdesinden alıyordu; yetkili bir kullanıcı başka kurumun ücretini değiştirebiliyordu.
/// İkisi de aynı kökün sonucuydu: parametre yanlış katmandaydı.</para>
///
/// <para>Yazma izni <c>platform:parameter:manage</c>; okuma <c>salary:parameter:view</c>.</para>
/// </remarks>
public class SalaryCalculationConfig
{
    public Guid Id { get; set; }
    public decimal MinimumWage { get; set; }

    /// <summary>
    /// 16 yaşından küçükler için belirlenen asgari ücret (#85). 3308 Madde 25 ve MEB Ortaöğretim
    /// Kurumları Yönetmeliği (6)(a) "YAŞINA UYGUN asgari ücret" diyor. null ise ayrı bir tutar
    /// belirlenmemiş demektir ve yaşa bakılmaksızın <see cref="MinimumWage"/> uygulanır.
    /// </summary>
    public decimal? MinimumWageUnder16 { get; set; }

    /// <summary>Aday çırak ve çırak taban oranı — Madde 25: "yüzde otuzundan aşağı olamaz" (#85).</summary>
    public decimal ApprenticeRate { get; set; } = 0.30m;
    public int PersonnelThreshold { get; set; } = 20;
    public decimal LargeBusinessRate { get; set; } = 0.30m;
    public decimal SmallBusinessRate { get; set; } = 0.15m;
    public decimal MEM12thGradeRate { get; set; } = 0.50m;
    // 3308 Geçici Madde 12 "üçte ikisi" / "üçte biri" diyor — tam kesir. Önceden 0.6667m ve
    // 0.3333m yazılıydı; kırpılmış değer öğrenci başına aylık ~0,22 TL sapma üretiyordu (#83).
    public decimal GovContribSmallNonMEM { get; set; } = 2m / 3m;
    public decimal GovContribLargeNonMEM { get; set; } = 1m / 3m;
    public decimal GovContribMEM { get; set; } = 1.0m;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    /// <summary>
    /// Son değişikliği yapan kullanıcının kimliği — token'dan gelir, istekten ALINMAZ (#137).
    /// Ad sorgu tarafında <c>UserNameView</c>'dan çözülür. Eski <c>updatedBy</c> JSON
    /// anahtarı (serbest metin ad) bu adla artık okunmaz.
    /// </summary>
    public Guid UpdatedById { get; set; }
}
