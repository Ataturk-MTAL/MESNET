using MESNET.Attendance.Core.Aggregates;

namespace MESNET.Attendance.Core.Policies;

/// <summary>Sayacın iki ayağı — bkz. <see cref="AttendanceLimitPolicy.Evaluate"/>.</summary>
/// <param name="UnexcusedDays">Mazeretsiz gün sayısı.</param>
/// <param name="TotalDays">Tüm devamsızlık günleri.</param>
public sealed record AttendanceTally(int UnexcusedDays, int TotalDays);

/// <summary>
/// Devamsızlık sayacının kapsamı: <b>öğrenci + akademik dönem</b> (#242), <b>yalnız hüküm
/// doğurmuş kayıtlar</b> (#252).
///
/// <para><b>Neden kritik:</b> bu sayaç doğrudan fesih tetikleyicisidir —
/// <c>AttendanceLimitExceeded</c> → <c>InternshipSaga</c> → fesih onay zinciri.</para>
///
/// <para><b>Yaşanan:</b> <c>AttendanceViewProjection</c> kimliği yalnız <c>StudentId</c>'ydi,
/// görünümde <c>AcademicPeriodId</c> alanı hiç yoktu ve <c>BusinessId</c> yalnız ilk olayda
/// yazılıyordu. İki hata birden doğuyordu ve ikisi de sessizdi:</para>
///
/// <list type="number">
///   <item>Aynı işletmede kalan öğrencide sayaç dönem başında <b>sıfırlanmıyordu</b> — geçen
///   yılın mazeretsiz günleri bu yılın eşiğine sayılıyor, öğrenci <b>erken</b> feshe gidiyordu.</item>
///   <item>Öğrenci işletme değiştirdiğinde handler'ın <c>BusinessId</c> eşleşmesi görünümü
///   <b>bulamıyordu</b>; <c>total</c> hep 1 kalıyor ve limit <b>bir daha hiç</b> tetiklenmiyordu.
///   Fesih→yeni yerleştirme akışından geçen her öğrencide kalıcıydı.</item>
/// </list>
///
/// <para><b>İşletme anahtara neden GİRMEZ:</b> girseydi öğrenci işletme değiştirince sayaç
/// sıfırlanır ve yıl içinde iki işletmede toplam 38 mazeretsiz gün biriktiren öğrenci hiçbir
/// eşiğe takılmazdı. Devamsızlık öğrencinin <b>eğitim yılına</b> ait bir kayıttır, işletmeye
/// değil.</para>
/// </summary>
public static class AttendanceCounterScope
{
    /// <summary>Onay bekleyen kayıt — <see cref="Core.Enums.AttendanceStatus.Pending"/> adı.</summary>
    private const string PendingStatusName = "Pending";

    /// <summary>
    /// Sayaç satırının anahtarı. <b>Okunabilir</b> tutuldu — operatörün veritabanında satırı
    /// gözle bulabilmesi gerekiyor; hash'lenmiş bir kimlik bunu imkânsız kılardı.
    /// </summary>
    /// <param name="businessId">
    /// <b>Kullanılmaz.</b> İmzada yalnız çağıranın "işletmeyi de vermeliyim" refleksini
    /// yakalamak ve kararın bilinçli olduğunu göstermek için var — bkz. sınıf özeti.
    /// </param>
    public static string KeyFor(Guid studentId, Guid academicPeriodId, Guid? businessId = null)
        => $"{studentId}:{academicPeriodId}";

    /// <summary>
    /// Eşik <b>dolduğunda</b> tetiklenir, aşıldığında değil — 20 limitte 20. gün fesih başlatır.
    /// Bugünkü davranış budur; #242 sayacın <i>kapsamını</i> düzeltir, eşik semantiğini değil.
    /// </summary>
    public static bool IsExceeded(int total, int limit) => total >= limit;

    /// <summary>
    /// Bu devamsızlık türü <b>mazeretsiz</b> sayaca mı yazılır (#183).
    ///
    /// <para>Yalnız <c>AbsenceType.Unexcused</c> mazeretsizdir. Sağlık raporu, mazeretli
    /// devamsızlık, ücretli ve ücretsiz izin <b>mazeretli</b> sayaca gider — ücret kesintisi
    /// ayrımıyla (<c>AbsenceType.AffectsSalary</c>) karıştırılmamalı: ücretsiz izinde ücret
    /// kesilir ama devamsızlık mazeretsiz değildir.</para>
    /// </summary>
    public static bool CountsAsUnexcused(string? absenceType)
        => string.Equals(absenceType, "Unexcused", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Bu <b>durumdaki</b> kayıt fesih sayacına girer mi (#252).
    ///
    /// <para><b>Onay bekleyen kayıt girmez.</b> İşletme yetkilisi, işletme İK ve usta öğretici
    /// devamsızlık <i>bildirir</i> (<c>attendance:upload</c>); girdiği kayıt <c>Pending</c>
    /// doğar ve hükmü koordinatör öğretmenin onayıyla doğar (#172 — "giriş geniş, hüküm dar").
    /// Sayaç duruma bakmazken işletme, öğretmen hiç dokunmadan fesih onay zincirini tek taraflı
    /// başlatabiliyordu; en ağır hüküm, hükmü olmayan girdiden çıkıyordu.</para>
    ///
    /// <para><b>Aynı ayrım para yolunda zaten vardı:</b>
    /// <c>PaymentSaga.CountDeductibleAbsenceDaysAsync</c> <c>Pending</c> kaydı ücret
    /// kesintisine saymıyor. Ödemeyi yapan taraf kendi kesintisini tek taraflı koyamıyorsa,
    /// sözleşmenin feshini de tek taraflı başlatamamalı.</para>
    ///
    /// <para><b>Neden kara liste — yalnız <c>Pending</c> dışlanıyor:</b> beyaz liste
    /// (<c>Recorded</c> + <c>Verified</c>) yazılsaydı <c>Corrected</c> sessizce sayaçtan
    /// düşerdi; düzeltilen kayıt hâlâ geçerli bir devamsızlıktır ve <c>Corrected</c> durumu
    /// <c>Pending</c>'e geri dönmez. Yeni bir durum eklendiğinde de beyaz liste onu sessizce
    /// yutar — sayacın eksik kalması, fazla saymaktan daha sessiz bir hatadır.</para>
    ///
    /// <para><b>Bilinmeyen/boş durum SAYILIR.</b> <c>AttendanceLimitPolicy</c> ile aynı yön:
    /// eksik veri sınırı gevşetemez. Uygulamada boş durum görülmez —
    /// <c>AttendanceRecord.Create</c> tanınmayan <c>InitialStatus</c>'ü <c>Recorded</c>'a
    /// düşürür — ama kural yönü açıkça yazılı olsun.</para>
    /// </summary>
    public static bool CountsTowardLimit(string? statusName)
        => !string.Equals(statusName, PendingStatusName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Kayıt kümesini sayacın iki ayağına indirir: <b>silinmiş</b> ve <b>onay bekleyen</b>
    /// kayıtlar elenir, kalanlar tür eksenine göre ayrılır.
    ///
    /// <para><b>Neden bellekte ve neden burada:</b> aynı karar hem bu sayaçta hem
    /// <c>AttendanceViewProjection</c>'da veriliyordu ve ayrışması "sınır yanlış ayaktan
    /// tetiklenir" demekti (#183). Karar saf bir fonksiyona çıkınca <b>testlenebilir</b> hâle
    /// de gelir: <c>CheckAttendanceLimitHandler</c> <c>IQuerySession</c> aldığı için veritabanı
    /// olmadan çağrılamıyor, dolayısıyla fesih eşiğinin kendisi test edilemiyordu.</para>
    ///
    /// <para><b>Süzme LINQ'te YAPILMAZ.</b> <c>StatusName</c>/<c>IsDeleted</c> alanları
    /// belgeye sonradan eklendi; alanı taşımayan eski snapshot'ta
    /// <c>data-&gt;&gt;'statusName' != 'Pending'</c> SQL'de <c>NULL</c> üretir ve satır
    /// <b>sessizce elenir</b> — sayaç eksik kalır, limit hiç tetiklenmez. Aynı tuzak tür
    /// ekseninde de var (SmartEnum nested path, bkz. CLAUDE.md). Küme bir öğrencinin bir
    /// eğitim yılıdır — sınırlı ve seyrek okunur.</para>
    /// </summary>
    public static AttendanceTally Tally(IEnumerable<AttendanceRecord> records)
    {
        var counted = records
            .Where(r => !r.IsDeleted && CountsTowardLimit(r.Status?.Name))
            .ToList();

        return new AttendanceTally(
            counted.Count(r => CountsAsUnexcused(r.Type?.Name)),
            counted.Count);
    }
}
