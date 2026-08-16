namespace MESNET.Attendance.Core.Policies;

/// <summary>
/// Devamsızlık sayacının kapsamı: <b>öğrenci + akademik dönem</b> (#242).
///
/// <para><b>Neden kritik:</b> bu sayaç doğrudan fesih tetikleyicisidir —
/// <c>AttendanceLimitExceeded</c> → <c>InternshipSaga</c> → otomatik fesih zinciri.</para>
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
    ///
    /// <para><b>Neden burada:</b> aynı karar hem <c>AttendanceViewProjection</c>'da (sayacı
    /// artırırken) hem <c>CheckAttendanceLimitHandler</c>'da (henüz yansımamış olayı sayarken)
    /// veriliyor. İkisi ayrışırsa sınır yanlış ayaktan tetiklenir ve bu <b>fesih</b> demektir.</para>
    /// </summary>
    public static bool CountsAsUnexcused(string? absenceType)
        => string.Equals(absenceType, "Unexcused", StringComparison.OrdinalIgnoreCase);
}
