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
        var limit = AttendanceLimitPolicy.LimitFor(student?.EducationType, config);
        var total = (view?.UnexcusedDays ?? 0) + 1;

        if (AttendanceCounterScope.IsExceeded(total, limit))
            return new AttendanceLimitExceeded(
                @event.StudentId, @event.InstitutionId, @event.BusinessId, total, limit,
                @event.AcademicPeriodId);

        return null;
    }
}
