using MESNET.Attendance.Core.Entities;

namespace MESNET.Attendance.Core.Policies;

/// <summary>Sınırın hangi ayaktan dolduğu — log ve fesih gerekçesi için.</summary>
/// <param name="IsExceeded">Fesih zinciri tetiklenmeli mi.</param>
/// <param name="Days">Sınırı dolduran gün sayısı.</param>
/// <param name="Limit">Dolan sınır.</param>
/// <param name="Kind">Hangi ayak — <c>Unexcused</c>, <c>Total</c> ya da <c>None</c>.</param>
public sealed record AttendanceLimitDecision(bool IsExceeded, int Days, int Limit, string Kind)
{
    public static readonly AttendanceLimitDecision NotExceeded = new(false, 0, 0, "None");
}

/// <summary>Fiilen uygulanan sınırlar — MESEM'in özürsüz karşılığı bilerek yoktur.</summary>
public sealed record AttendanceLimits(
    int FormalUnexcusedDayLimit, int FormalTotalDayLimit, int MesemTotalDayLimit);

/// <summary>
/// İşletmede mesleki eğitimde devamsızlık sınırı — <b>eğitim türü VE devamsızlık türü</b>
/// ayrımıyla (#183).
///
/// <para><b>Yasal dayanak:</b> MEB Ortaöğretim Kurumları Yönetmeliği (18812) <b>md. 36 (5)</b>
/// ve 3308 sayılı Kanun <b>md. 26</b>. Sınır aşılınca yönetmeliğin öngördüğü sonuç
/// <i>"sözleşmeleri fesih edilerek sigorta çıkışları yapılır"</i>dır — sistemdeki karşılığı
/// <c>AttendanceLimitExceeded</c> → <c>InternshipSaga</c> → otomatik fesih. Yani buradaki her
/// sayı doğrudan bir <b>fesih tetikleyicisidir</b>.</para>
///
/// <para><b>Örgünde İKİ AYAK vardır ve ikisi de bağlayıcıdır:</b> md. 36 (5)
/// <i>"Devamsızlık süresi <b>özürsüz 10 günü</b>, <b>toplamda 30 günü</b> aşan öğrenciler, ders
/// puanları ne olursa olsun başarısız sayılır"</i> der. Yalnız mazeretsizi saymak, 29 gün
/// raporlu + 9 gün mazeretsiz olan öğrenciyi sınırın dışında bırakırdı — oysa toplam ayağı
/// çoktan dolmuştur.</para>
///
/// <para><b>MESEM'de tek ayak vardır ve o TOPLAMDIR:</b> md. 36 (5) işletme eğitimi için
/// <i>"3308 sayılı Kanun hükümlerine göre kullanabileceği <b>ücretli ve ücretsiz izin
/// toplamından</b> fazla olamaz"</i> der — devamsızlık türüne bakmaz, izin hakkıyla
/// karşılaştırır. Ayrı bir mazeretsiz ayağı <b>getirilmemiştir</b> ve uydurulmadı.</para>
///
/// <para><b>PARAMETRİKTİR — mevzuat değişirse kod değişmez.</b> Yürürlükteki değerler
/// <see cref="AttendanceLimitConfig"/> belgesinden gelir; belge <b>ulusal parametredir</b>
/// (<c>platform:parameter:manage</c>, #147 deseni), kurum ayarı değildir. Aşağıdaki sabitler
/// yalnız <b>başlangıç değerleridir</b>: kayıt hiç girilmemişken kullanılır — "yapılandırma yok"
/// hâli sınırın sessizce kalkması anlamına gelemez.</para>
///
/// <para><b>Önceki hâli:</b> <c>const int limit = 20</c> — hiçbir hükümle eşleşmiyordu.</para>
/// </summary>
public static class AttendanceLimitPolicy
{
    /// <summary>Mazeretsiz ayak — <c>AttendanceLimitDecision.Kind</c> değeri.</summary>
    public const string UnexcusedKind = "Unexcused";

    /// <summary>Toplam ayak — <c>AttendanceLimitDecision.Kind</c> değeri.</summary>
    public const string TotalKind = "Total";

    /// <summary>Md. 36 (5): örgünde <b>özürsüz</b> ayak.</summary>
    public const int FormalUnexcusedDayLimit = 10;

    /// <summary>Md. 36 (5): örgünde <b>toplam</b> ayak — mazeretli, raporlu ve izinli dâhil.</summary>
    public const int FormalTotalDayLimit = 30;

    /// <summary>
    /// MESEM'de <b>toplam</b> ayak. Md. 36 (5) → 3308 md. 26: yılda tatil aylarında 1 ay ücretli
    /// izin + okul müdürlüğü görüşüyle 1 aya kadar ücretsiz izin. Ay uzunluğu deponun kendi
    /// kuralıyla (SGK usulü 30 günlük ay, business-rules.md §6.2.1): 30 + 30 = 60.
    /// </summary>
    public const int MesemTotalDayLimit = 60;

    /// <summary>
    /// Sınır dolmuş mu — <b>her iki ayak da</b> değerlendirilir ve <b>önce dolan</b> tetikler.
    /// </summary>
    /// <param name="educationTypeName">
    /// Bilinmeyen/boş değer <b>örgün</b> sayılır: daha dar sınırlar, yani daha erken tetikleme.
    /// Eksik veri sınırı gevşetemez.
    /// </param>
    /// <param name="unexcusedDays">Mazeretsiz gün sayısı (<c>AbsenceType.Unexcused</c>).</param>
    /// <param name="totalDays">Tüm devamsızlık günleri — mazeretli, raporlu ve izinli dâhil.</param>
    public static AttendanceLimitDecision Evaluate(
        string? educationTypeName, int unexcusedDays, int totalDays, AttendanceLimitConfig? config = null)
    {
        var limits = EffectiveLimits(config);

        if (IsMesem(educationTypeName))
        {
            // MESEM'de mazeretsiz ayağı YOKTUR — yönetmelik işletme eğitimini yalnız izin
            // hakkıyla karşılaştırır. Olmayan bir ayak uydurulmadı.
            return AttendanceCounterScope.IsExceeded(totalDays, limits.MesemTotalDayLimit)
                ? new AttendanceLimitDecision(true, totalDays, limits.MesemTotalDayLimit, TotalKind)
                : AttendanceLimitDecision.NotExceeded;
        }

        // Sıra: mazeretsiz ayak önce sorulur. İkisi aynı anda dolduğunda gerekçe "özürsüz"
        // yazılır — idarenin göreceği sebep, öğrencinin sorumlu olduğu ayak olmalı.
        if (AttendanceCounterScope.IsExceeded(unexcusedDays, limits.FormalUnexcusedDayLimit))
            return new AttendanceLimitDecision(
                true, unexcusedDays, limits.FormalUnexcusedDayLimit, UnexcusedKind);

        if (AttendanceCounterScope.IsExceeded(totalDays, limits.FormalTotalDayLimit))
            return new AttendanceLimitDecision(
                true, totalDays, limits.FormalTotalDayLimit, TotalKind);

        return AttendanceLimitDecision.NotExceeded;
    }

    /// <summary>
    /// <b>Fiilen uygulanan</b> sınırlar — yapılandırma bozuk ya da eksikse başlangıç değerleri.
    /// Arayüz bu değerleri gösterir; kayıttaki ham sayıyı göstermek, idarenin uygulanmayan bir
    /// sınıra göre karar vermesi demek olurdu.
    /// </summary>
    public static AttendanceLimits EffectiveLimits(AttendanceLimitConfig? config) => new(
        Effective(config?.FormalUnexcusedDayLimit, FormalUnexcusedDayLimit),
        Effective(config?.FormalTotalDayLimit, FormalTotalDayLimit),
        Effective(config?.MesemTotalDayLimit, MesemTotalDayLimit));

    private static bool IsMesem(string? educationTypeName)
        => string.Equals(educationTypeName, "Mesem", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Bozuk/eksik yapılandırma sınırı <b>kaldırmaz</b>, başlangıç değerine düşer. 0 ya da
    /// negatif bir sınır "her öğrenci ilk devamsızlıkta feshedilir" demek olurdu — #151'in yeter
    /// sayı eşiğinde aynı tuzak aynı gerekçeyle kapatılmıştı.
    /// </summary>
    private static int Effective(int? configured, int fallback) =>
        configured is > 0 ? configured.Value : fallback;
}
