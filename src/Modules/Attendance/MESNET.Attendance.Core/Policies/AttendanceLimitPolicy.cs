namespace MESNET.Attendance.Core.Policies;

/// <summary>
/// İşletmede mesleki eğitimde devamsızlık sınırı — <b>eğitim türüne göre</b> (#183).
///
/// <para><b>Yasal dayanak:</b> MEB Ortaöğretim Kurumları Yönetmeliği (18812) <b>md. 36</b> ve
/// 3308 sayılı Kanun <b>md. 26</b>. Sınır aşılınca yönetmeliğin öngördüğü sonuç
/// <i>"sözleşmeleri fesih edilerek sigorta çıkışları yapılır"</i>dır — sistemdeki karşılığı
/// <c>AttendanceLimitExceeded</c> → <c>InternshipSaga</c> → otomatik fesih zinciridir. Yani bu
/// sınıftaki sayı doğrudan bir <b>fesih tetikleyicisidir</b>.</para>
///
/// <para><b>Önceki hâli:</b> <c>const int limit = 20</c> — mevzuatta karşılığı <b>hiçbir
/// hükümle eşleşmeyen</b> uydurulmuş bir sayıydı: ne 10, ne 30, ne 1/6, ne izin toplamı.</para>
///
/// <para><b>Bu sınıf mevzuat türevidir, kurum ayarı DEĞİLDİR.</b> Okul başına değişmemeli;
/// değişmesi gerekiyorsa mevzuat değişmiştir ve tek yer burasıdır. (#183'te kararlaştırılan
/// yön ulusal parametre katmanıydı — #147 deseni; oraya taşınana kadar sabitler burada, tek
/// noktada ve gerekçeli durur.)</para>
/// </summary>
public static class AttendanceLimitPolicy
{
    /// <summary>
    /// <b>Örgün ortaöğretim — 10 gün.</b>
    ///
    /// <para>Md. 36 (5): <i>"Devamsızlık süresi özürsüz 10 günü, toplamda 30 günü aşan
    /// öğrenciler, ders puanları ne olursa olsun başarısız sayılır."</i></para>
    ///
    /// <para><b>TÜRETİM — dikkat.</b> Bu fıkra <b>okul</b> devamı içindir; yönetmelik örgün
    /// öğrencinin <b>işletmedeki</b> devamsızlığı için ayrı bir sınır <b>getirmiyor</b>. Sistem
    /// yalnız mazeretsiz günleri saydığı için (<c>AbsenceType.Unexcused</c>) fıkranın
    /// <b>özürsüz</b> ayağı seçildi: <b>10</b>. Toplam 30 rakamı mazeretli günleri de kapsar ve
    /// bu sayaç onları saymaz.</para>
    ///
    /// <para>⚠ Bu, eski 20'ye göre eşiği <b>yarıya indirir</b> — daha önce tetiklenmeyen
    /// durumlar artık fesih zincirini başlatır. Bilinçli: 20 hiçbir hükme dayanmıyordu.</para>
    /// </summary>
    public const int FormalUnexcusedDayLimit = 10;

    /// <summary>
    /// <b>MESEM — 60 gün.</b>
    ///
    /// <para>Md. 36 (5): <i>"…işletmede mesleki eğitimde ise 3308 sayılı Kanun hükümlerine göre
    /// kullanabileceği ücretli ve ücretsiz izin toplamından fazla olamaz."</i></para>
    ///
    /// <para><b>TÜRETİM.</b> 3308 md. 26: yılda tatil aylarında <b>bir ay ücretli izin</b>,
    /// mazereti kabul edilenlere okul müdürlüğünün görüşüyle <b>bir aya kadar ücretsiz izin</b>.
    /// Ay uzunluğu için deponun kendi kuralı kullanıldı — SGK usulü <b>30 günlük ay</b>
    /// (business-rules.md §6.2.1). Toplam: 30 + 30 = <b>60</b>.</para>
    ///
    /// <para><b>Bilinen sapma:</b> yönetmelik sınırı <b>toplam devamsızlığa</b> koyuyor, bu
    /// sayaç ise yalnız <b>mazeretsiz</b> günleri sayıyor. Yani gerçek sınır bundan daha erken
    /// dolabilir. Mazeretli/raporlu günleri de sayan bir ölçüm ayrı bir iştir; buradaki seçim
    /// <b>öğrenci lehine</b> (geç tetikleyen) yöndedir — yanlış fesih, geç fesihten pahalıdır.</para>
    ///
    /// <para>Sahibin verdiği <b>"6 gün"</b> bu sınır DEĞİLDİR: o, md. 36'nın <b>teorik ders</b>
    /// için koyduğu <i>"devam etmesi gereken sürenin altıda biri"</i> kuralının, haftada bir gün
    /// okul olan MESEM öğrencisinde ~36 ders gününe uygulanmasıyla çıkan sayıdır. Teorik ders
    /// devamı <b>okul</b> devamıdır ve bu sistemin ölçtüğü şey değildir (e-Okul'un alanı).</para>
    /// </summary>
    public const int MesemUnexcusedDayLimit = 60;

    /// <summary>
    /// Eğitim türüne göre sınırı çözer.
    ///
    /// <para><b>Bilinmeyen tür örgün sayılır</b> — daha <b>düşük</b> eşik, yani daha erken
    /// tetikleme. Eksik veri yüzünden sınırın sessizce gevşemesi, öğrencinin sınırsız
    /// devamsızlık yapabilmesi demek olurdu.</para>
    /// </summary>
    public static int LimitFor(string? educationTypeName) =>
        string.Equals(educationTypeName, "Mesem", StringComparison.OrdinalIgnoreCase)
            ? MesemUnexcusedDayLimit
            : FormalUnexcusedDayLimit;
}
