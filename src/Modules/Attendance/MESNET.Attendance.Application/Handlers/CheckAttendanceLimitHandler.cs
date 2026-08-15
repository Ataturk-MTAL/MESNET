using Marten;
using MESNET.Attendance.Core.Entities;
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

        // TODO(#183): limit sabit kodlanmış. Karar verildi — ulusal parametre katmanından
        // (platform:parameter:manage, #147 deseni) çözülecek ve eğitim türüne göre değişebilir.
        // Mevzuat teyidi (MEB Yönetmeliği md. 36 + MESEM yönergesi) bekleniyor; teyit gelmeden
        // sayı değiştirilmiyor çünkü bu değer doğrudan fesih tetikleyicisidir.
        const int limit = 20;
        var total = (view?.UnexcusedDays ?? 0) + 1;

        if (AttendanceCounterScope.IsExceeded(total, limit))
            return new AttendanceLimitExceeded(
                @event.StudentId, @event.InstitutionId, @event.BusinessId, total, limit,
                @event.AcademicPeriodId);

        return null;
    }
}
