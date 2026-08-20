using Marten;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Core.Policies;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Devamsızlık sınırını değerlendirir ve aşıldığında fesih zincirini tetikler.
///
/// <para><b>Sayaç ASENKRON GÖRÜNÜMDEN OKUNMAZ (#249).</b> Eskiden <c>AttendanceView</c>
/// okunuyordu; o projeksiyon <c>ProjectionLifecycle.Async</c>'tir ve işlenmekte olan olay için
/// yalnız <c>+1</c> telafisi vardı. Ardışık girişte daemon birden çok olay geride kalıyor,
/// sayaç eksik okunuyor ve <b>sınır sessizce atlanıyordu</b>. Ölçüldü: 3 senaryonun 17 kaydı
/// arka arkaya girildiğinde hiçbiri tetiklemedi; görünüm oturduktan sonra tek kayıt eklenince
/// ikisi de anında tetikledi. Okul haftalık devamsızlığı tek oturumda girer — yani bu, kenar
/// durum değil <b>normal kullanım</b>dı.</para>
///
/// <para><b>Neden <see cref="AttendanceRecord"/>:</b> anlık görüntüsü <c>Inline</c>'dır, yani
/// devamsızlığın yazıldığı transaction'da güncellenir. Bu handler cascading mesajla, o
/// transaction commit olduktan SONRA çalışır — kayıt kesinlikle görünürdür. Bayatlık ihtimali
/// ortadan kalkar, <c>+1</c> telafisine de gerek kalmaz.</para>
///
/// <para><b>Yan kazanç:</b> düzeltilen ve silinen kayıtlar da doğru sayılır. Görünüm
/// <c>AttendanceCorrected</c>/<c>AttendanceDeleted</c> olaylarını uygulayamıyor (olaylar
/// <c>AcademicPeriodId</c> taşımıyor, düzeltme eski türü taşımıyor), bu yüzden yanlış girilip
/// sonra düzeltilen devamsızlık sayaçta kalıyordu. Aggregate'te tür ve <c>IsDeleted</c>
/// günceldir.</para>
///
/// <para><b>ÜÇ GİRİŞ NOKTASI VARDIR (#252)</b> — sayılabilir küme her değiştiğinde yeniden
/// değerlendirilir: kayıt <b>girildiğinde</b>, <b>onaylandığında</b> ve <b>düzeltildiğinde</b>.
/// Yalnız <c>AttendanceMarked</c> dinlenseydi onay bekleyen kaydın dışlanması (aşağı bak)
/// açığı kapatırken yerine ters yönde bir açık açardı: işletmenin bildirdiği devamsızlık
/// öğretmen onayladıktan sonra da <b>hiçbir zaman</b> sayılmaz, mevzuatın emrettiği fesih
/// sessizce hiç tetiklenmezdi. Süre dolunca kendiliğinden onay <b>yoktur</b> — belgede
/// (<c>business-rules.md</c> §5.7) tarif edilen <c>AutoApproveExpiredAttendance</c> işi kodda
/// yazılmamıştır, yani <c>Pending</c> kayıt onaylanana kadar <c>Pending</c> kalır.</para>
/// </summary>
public static class CheckAttendanceLimitHandler
{
    // Marten 9 senkron veri erişimini kaldırdı — .FirstOrDefault() burada
    // "As of Marten 9.0, only asynchronous data access is supported" fırlatıyordu ve
    // AttendanceMarked dead letter'a düşüyordu, yani devamsızlık limiti hiç kontrol edilmiyordu (#73).
    public static Task<AttendanceLimitExceeded?> Handle(AttendanceMarked @event, IQuerySession session)
        => EvaluateAsync(session, @event.StudentId, @event.AcademicPeriodId,
            @event.InstitutionId, @event.BusinessId);

    /// <summary>
    /// Onay, kaydı sayaca <b>sokan</b> olaydır (#252) — bu yüzden sınır burada yeniden ölçülür.
    /// Öğretmen haftanın biriken bildirimlerini toplu onayladığında eşik tam bu anda dolar.
    /// </summary>
    public static Task<AttendanceLimitExceeded?> Handle(AttendanceApproved @event, IQuerySession session)
        => EvaluateForRecordAsync(session, @event.AttendanceId);

    /// <summary>
    /// Düzeltme <b>türü</b> değiştirir (ör. mazeretli → mazeretsiz) ve mazeretsiz ayağı 10 günde
    /// dolduğu için sınırı düzeltmenin kendisi doldurabilir. Kontrol yoksa tür değişimi
    /// hükümsüz kalırdı.
    /// </summary>
    public static Task<AttendanceLimitExceeded?> Handle(AttendanceCorrected @event, IQuerySession session)
        => EvaluateForRecordAsync(session, @event.AttendanceId);

    /// <summary>
    /// Olay yalnız <c>AttendanceId</c> taşıyor; sayacın anahtarı (öğrenci + dönem) ve olayın
    /// gerektirdiği kurum/işletme kimliği <b>aggregate'ten</b> okunur. Olaya alan eklemek yerine
    /// bu yol seçildi: olaylar <c>shared.mt_events</c> içinde kalıcıdır ve saklı olayların
    /// şekli değiştirilemez; aggregate'in <c>Inline</c> snapshot'ı ise her zaman günceldir.
    /// </summary>
    private static async Task<AttendanceLimitExceeded?> EvaluateForRecordAsync(
        IQuerySession session, Guid attendanceId)
    {
        var record = await session.LoadAsync<AttendanceRecord>(attendanceId);
        if (record is null) return null;

        return await EvaluateAsync(session, record.StudentId, record.AcademicPeriodId,
            record.InstitutionId, record.BusinessId);
    }

    private static async Task<AttendanceLimitExceeded?> EvaluateAsync(
        IQuerySession session, Guid studentId, Guid academicPeriodId,
        Guid institutionId, Guid businessId)
    {
        // Kapsam öğrenci + akademik dönem başınadır (#242). İşletme anahtara GİRMEZ: girseydi
        // öğrenci işletme değiştirince sayaç sıfırlanır ve yıl içinde iki işletmede biriken
        // devamsızlık hiçbir eşiğe takılmazdı.
        //
        // Sorgu BİLEREK geniş: silinmiş ve onay bekleyen kayıtların elenmesi bellekte, saf
        // AttendanceCounterScope.Tally içinde yapılır. LINQ'te süzülseydi alanı taşımayan eski
        // snapshot'lar SQL'de NULL üretip sessizce elenir ve sayaç eksik kalırdı — gerekçe
        // Tally'nin özetinde.
        var records = await session.Query<AttendanceRecord>()
            .Where(r => r.StudentId == studentId && r.AcademicPeriodId == academicPeriodId)
            .ToListAsync();

        // Tür ve durum ayrımı bellekte yapılır: AbsenceType/AttendanceStatus birer SmartEnum ve
        // Marten LINQ'inde r.Type.Name → data->'type'->>'Name' üretip her zaman NULL döner
        // (bkz. CLAUDE.md). Küme bir öğrencinin bir eğitim yılıdır — sınırlı ve seyrek okunur.
        var tally = AttendanceCounterScope.Tally(records);

        // Sınır artık EĞİTİM TÜRÜNE ve DEVAMSIZLIK TÜRÜNE göre, mevzuattan türetiliyor (#183).
        // Öğrenci kaydı bulunamazsa tür bilinmez ve politika DAHA DAR eşiklere düşer; eksik
        // veri sınırı gevşetmemeli.
        var student = await session.LoadAsync<StudentNameView>(studentId);

        // Sınır ULUSAL PARAMETREDİR (#183): mevzuat değişirse kod değil kayıt değişir.
        // Kayıt yoksa ya da bozuksa politika mevzuattan türetilmiş başlangıç değerine düşer —
        // "yapılandırma yok" sınırın kalkması anlamına gelemez.
        var config = await session.LoadAsync<AttendanceLimitConfig>(AttendanceLimitConfig.SingletonId);

        var decision = AttendanceLimitPolicy.Evaluate(
            student?.EducationType, tally.UnexcusedDays, tally.TotalDays, config);

        if (decision.IsExceeded)
            return new AttendanceLimitExceeded(
                studentId, institutionId, businessId,
                decision.Days, decision.Limit, academicPeriodId, decision.Kind);

        return null;
    }
}
