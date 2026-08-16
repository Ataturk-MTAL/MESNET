using Marten;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Core.Policies;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Application.Handlers;

public static class CheckAttendanceLimitHandler
{
    // Marten 9 senkron veri erişimini kaldırdı — .FirstOrDefault() burada
    // "As of Marten 9.0, only asynchronous data access is supported" fırlatıyordu ve
    // AttendanceMarked dead letter'a düşüyordu, yani devamsızlık limiti hiç kontrol edilmiyordu (#73).
    public static async Task<AttendanceLimitExceeded?> Handle(
        AttendanceMarked @event, IQuerySession session)
    {
        // Sayaç öğrenci + akademik dönem başınadır (#242). Eskiden sorgu BusinessId ile
        // eşleşiyordu; öğrenci işletme değiştirince satır bulunamıyor, total hep 1 kalıyor ve
        // limit BİR DAHA HİÇ tetiklenmiyordu — fesih→yeni yerleştirme akışından geçen her
        // öğrencide kalıcı olarak.
        var key = AttendanceCounterScope.KeyFor(@event.StudentId, @event.AcademicPeriodId);
        var view = await session.LoadAsync<AttendanceView>(key);

        // Sınır artık EĞİTİM TÜRÜNE göre ve MEVZUATTAN türetiliyor (#183) — eski sabit 20
        // hiçbir hükümle eşleşmiyordu. Gerekçe ve dayanak: AttendanceLimitPolicy.
        // Öğrenci kaydı bulunamazsa tür bilinmez ve politika DAHA DÜŞÜK eşiğe düşer; eksik
        // veri sınırı gevşetmemeli.
        var student = await session.LoadAsync<StudentNameView>(@event.StudentId);

        // Sınır ULUSAL PARAMETREDİR (#183): mevzuat değişirse kod değil kayıt değişir.
        // Kayıt yoksa ya da bozuksa politika mevzuattan türetilmiş başlangıç değerine düşer —
        // "yapılandırma yok" sınırın kalkması anlamına gelemez.
        var config = await session.LoadAsync<AttendanceLimitConfig>(AttendanceLimitConfig.SingletonId);

        // İKİ AYAK birden değerlendirilir (#183). Md. 36 (5) örgünde "özürsüz 10 günü,
        // toplamda 30 günü" der; yalnız mazeretsizi saymak, 29 gün raporlu + 9 gün mazeretsiz
        // olan öğrenciyi sınırın DIŞINDA bırakıyordu — oysa toplam ayağı çoktan dolmuştu.
        //
        // +1: görünüm asenkron güncellenir, bu olay ona henüz yansımadı. Hangi sayaca
        // yazılacağı projeksiyonla AYNI politikadan sorulur; ayrışırlarsa sınır yanlış ayaktan
        // tetiklenir ve sonucu fesihtir.
        var isUnexcused = AttendanceCounterScope.CountsAsUnexcused(@event.AbsenceType);
        var unexcusedDays = (view?.UnexcusedDays ?? 0) + (isUnexcused ? 1 : 0);
        var totalDays = (view?.TotalAbsenceDays ?? 0) + 1;

        var decision = AttendanceLimitPolicy.Evaluate(
            student?.EducationType, unexcusedDays, totalDays, config);

        if (decision.IsExceeded)
            return new AttendanceLimitExceeded(
                @event.StudentId, @event.InstitutionId, @event.BusinessId,
                decision.Days, decision.Limit, @event.AcademicPeriodId, decision.Kind);

        return null;
    }
}
