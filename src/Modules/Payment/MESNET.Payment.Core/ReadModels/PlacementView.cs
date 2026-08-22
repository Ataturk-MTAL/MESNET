namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Payment modülünün yerel yerleştirme kaydı — Enrollment olaylarından beslenir.
/// Aylık maaş zamanlayıcısı kimler için ödeme açacağını buradan bulur (#63).
/// </summary>
/// <remarks>
/// Maaş devamsızlığa değil aya bağlı hesaplanmalı: devamsızlığı olmayan öğrenci de ücretini
/// almalı. Bunun için "o ay staj yapan öğrenciler" listesi gerekiyor; modüller arası doğrudan
/// DB erişimi yasak olduğu için Enrollment'ın yerleştirme verisi burada denormalize tutulur.
/// </remarks>
public class PlacementView
{
    public Guid Id { get; set; }       // PlacementId
    public Guid StudentId { get; set; }
    /// <summary>İşletme — okulda stajda null (#159).</summary>
    public Guid? BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    /// <summary>
    /// Yerleştirmenin başlangıç anı. Fesih olayı yerleştirme kimliği taşımadığı için
    /// (yalnız <c>StudentId</c> + <c>BusinessId</c>), aynı öğrencinin aynı işletmedeki
    /// ESKİ ve YENİ yerleştirmesini ayırmak buna bakılarak yapılır (#152).
    ///
    /// <para>Bu alandan önce yazılmış kayıtlarda <c>DateTime.MinValue</c> olur; o kayıtlar
    /// her fesihten önce sayılır ve kapatılırlar — istenen davranış budur, çünkü feshedilen
    /// zaten onlardır.</para>
    /// </summary>
    public DateTime PlacedAt { get; set; }

    /// <summary>Fesih/ayrılma sonrası false — o öğrenci için maaş açılmaz.</summary>
    public bool IsActive { get; set; } = true;
}
