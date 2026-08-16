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
/// </summary>
public static class CheckAttendanceLimitHandler
{
    // Marten 9 senkron veri erişimini kaldırdı — .FirstOrDefault() burada
    // "As of Marten 9.0, only asynchronous data access is supported" fırlatıyordu ve
    // AttendanceMarked dead letter'a düşüyordu, yani devamsızlık limiti hiç kontrol edilmiyordu (#73).
    public static async Task<AttendanceLimitExceeded?> Handle(
        AttendanceMarked @event, IQuerySession session)
    {
        // Kapsam öğrenci + akademik dönem başınadır (#242). İşletme anahtara GİRMEZ: girseydi
        // öğrenci işletme değiştirince sayaç sıfırlanır ve yıl içinde iki işletmede biriken
        // devamsızlık hiçbir eşiğe takılmazdı.
        var records = await session.Query<AttendanceRecord>()
            .Where(r => r.StudentId == @event.StudentId
                        && r.AcademicPeriodId == @event.AcademicPeriodId
                        && !r.IsDeleted)
            .ToListAsync();

        // Tür ayrımı bellekte yapılır: AbsenceType bir SmartEnum ve Marten LINQ'inde
        // r.Type.Name → data->'type'->>'Name' üretip her zaman NULL döner (bkz. CLAUDE.md).
        // Küme bir öğrencinin bir eğitim yılıdır — sınırlı ve seyrek okunur.
        var totalDays = records.Count;
        var unexcusedDays = records.Count(r => AttendanceCounterScope.CountsAsUnexcused(r.Type.Name));

        // Sınır artık EĞİTİM TÜRÜNE ve DEVAMSIZLIK TÜRÜNE göre, mevzuattan türetiliyor (#183).
        // Öğrenci kaydı bulunamazsa tür bilinmez ve politika DAHA DAR eşiklere düşer; eksik
        // veri sınırı gevşetmemeli.
        var student = await session.LoadAsync<StudentNameView>(@event.StudentId);

        // Sınır ULUSAL PARAMETREDİR (#183): mevzuat değişirse kod değil kayıt değişir.
        // Kayıt yoksa ya da bozuksa politika mevzuattan türetilmiş başlangıç değerine düşer —
        // "yapılandırma yok" sınırın kalkması anlamına gelemez.
        var config = await session.LoadAsync<AttendanceLimitConfig>(AttendanceLimitConfig.SingletonId);

        var decision = AttendanceLimitPolicy.Evaluate(
            student?.EducationType, unexcusedDays, totalDays, config);

        if (decision.IsExceeded)
            return new AttendanceLimitExceeded(
                @event.StudentId, @event.InstitutionId, @event.BusinessId,
                decision.Days, decision.Limit, @event.AcademicPeriodId, decision.Kind);

        return null;
    }
}
