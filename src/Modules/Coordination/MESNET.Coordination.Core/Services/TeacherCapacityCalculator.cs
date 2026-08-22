namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Tek öğretmenin kapasite girdisi (issue #116, <c>C</c> hesabı).
/// Saf veri taşıyıcı — çağıran katman kendi projeksiyonundan doldurur.
/// </summary>
/// <param name="TeacherId">Öğretmen kimliği (yalnız tanılama/izlenebilirlik için).</param>
/// <param name="FreeSlotTotal">
/// Ders programındaki boş slot toplamı — <c>TeacherSummaryRowDto.FreeSlotsByDay</c>
/// değerlerinin toplamı. Koordinatörlük ziyareti boş slota yerleşir, dolu slota değil.
/// </param>
/// <param name="AssignedBillableHours">
/// Öğretmene halihazırda takdir edilmiş <b>ücret doğuran</b> saat
/// (<c>TeacherSummaryRowDto.AssignedHours</c>). Fahri ziyaretler ek ders kotasına
/// girmediği için bu toplamda yoktur (#115).
/// </param>
public sealed record TeacherCapacityInput(
    Guid TeacherId,
    int FreeSlotTotal,
    int AssignedBillableHours);

/// <summary>
/// Alan öğretmenlerinin <b>kalan</b> koordinatörlük kapasitesini (<c>C</c>) hesaplar (issue #116).
///
/// <para><c>C = Σ_öğretmen max(0, min(boş slot toplamı, MaxWeeklyExtraHours − mevcut atanmış))</c></para>
///
/// <para>İki ayrı tavan aynı anda bağlar ve <b>küçüğü</b> geçerlidir:</para>
/// <list type="number">
///   <item><description>
///     <b>Boş slot</b> — ziyaret ders programında bir slot işgal eder; boş slotu olmayan
///     öğretmene havuzdan saat verilemez.
///   </description></item>
///   <item><description>
///     <b>Ek ders kotası</b> — <c>CoordinationConfig.MaxWeeklyExtraHours</c> öğretmen başına
///     azami haftalık ek ders saatidir; mevcut atanmış saat bu kotadan düşer.
///   </description></item>
/// </list>
///
/// <para><b>Anlam:</b> sonuç mutlak kapasite değil, <b>o an fazladan soğurulabilecek</b>
/// saattir. Zaten takdir edilmiş saatler hem boş slotları hem kotayı tüketmiş durumdadır;
/// bu yüzden dolu bir alanda <c>C</c> küçülür ve havuzun kalan kısmı alan dışına önerilir.
/// Kova ayrımının (<c>InBranchPaid</c> / <c>OutOfBranchSuggested</c>) dayandığı büyüklük budur.</para>
///
/// <para>Saf fonksiyon: dış bağımlılık yok, girdiyi değiştirmez, tamamen birim testli.</para>
/// </summary>
public static class TeacherCapacityCalculator
{
    /// <summary>
    /// Alan öğretmenlerinin toplam kalan kapasitesi.
    /// </summary>
    /// <param name="teachers">
    /// Alanın öğretmenleri — <b>öğretmen başına tek satır</b> beklenir. Yinelenen satır
    /// gelirse kapasite iki kez sayılır; ayıklama çağıranın sorumluluğundadır.
    /// </param>
    /// <param name="maxWeeklyExtraHours">
    /// Öğretmen başına azami haftalık ek ders saati. <c>0</c> veya altı → kapasite yok
    /// (yapılandırma okunamadığında da bu değer gelir; sessizce "sınırsız" varsayılmaz).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="teachers"/> null ise.</exception>
    public static int Calculate(IReadOnlyList<TeacherCapacityInput> teachers, int maxWeeklyExtraHours)
    {
        ArgumentNullException.ThrowIfNull(teachers);

        if (maxWeeklyExtraHours <= 0) return 0;

        return teachers.Sum(teacher => CapacityOf(teacher, maxWeeklyExtraHours));
    }

    /// <summary>
    /// Tek öğretmenin kalan kapasitesi. Negatif sonuç (kotası aşılmış öğretmen)
    /// toplamı aşağı çekmesin diye 0'a kırpılır.
    /// </summary>
    public static int CapacityOf(TeacherCapacityInput teacher, int maxWeeklyExtraHours)
    {
        ArgumentNullException.ThrowIfNull(teacher);

        if (maxWeeklyExtraHours <= 0) return 0;

        var quotaLeft = maxWeeklyExtraHours - Math.Max(0, teacher.AssignedBillableHours);
        var freeSlots = Math.Max(0, teacher.FreeSlotTotal);

        return Math.Max(0, Math.Min(freeSlots, quotaLeft));
    }
}
