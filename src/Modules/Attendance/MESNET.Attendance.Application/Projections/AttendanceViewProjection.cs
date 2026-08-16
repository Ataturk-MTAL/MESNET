using Marten.Events.Projections;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Policies;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Application.Projections;

/// <summary>
/// Devamsızlık sayacı — <b>öğrenci + akademik dönem</b> başına tek satır (#242).
///
/// <para><b>Önceki hâli iki hata üretiyordu:</b> kimlik yalnız <c>StudentId</c>'ydi, görünümde
/// <c>AcademicPeriodId</c> hiç yoktu ve <c>BusinessId</c> yalnız <c>Create</c>'te yazılıyordu.
/// Sayaç dönem başında sıfırlanmıyor (erken fesih), işletme değişince de handler'ın sorgusu
/// satırı bulamıyordu (limit bir daha hiç tetiklenmiyor). Ayrıntı:
/// <see cref="AttendanceCounterScope"/>.</para>
///
/// <para><b>Düzeltme/silme uygulanmıyor — bilinçli ve engelli.</b> <c>AttendanceCorrected</c>
/// ve <c>AttendanceDeleted</c> olayları <c>AcademicPeriodId</c> <b>taşımıyor</b>, yani bu
/// projeksiyon onları doğru satıra yönlendiremez. Üstelik <c>AttendanceCorrected</c> <b>eski</b>
/// devamsızlık türünü de taşımıyor; hangi sayacın düşeceği olaydan bilinemez. İkisi de olay
/// şeklinin değişmesini gerektirir (sona alan ekleme, <c>AttendanceLimitExceeded</c>'de
/// yapıldığı gibi) ve ayrı bir iştir. Bugünkü sonuç: yanlışlıkla girilip sonra düzeltilen
/// devamsızlık sayaçta kalır.</para>
/// </summary>
public partial class AttendanceViewProjection : MultiStreamProjection<AttendanceView, string>
{
    public AttendanceViewProjection()
    {
        Identity<AttendanceMarked>(e => AttendanceCounterScope.KeyFor(e.StudentId, e.AcademicPeriodId));
        Identity<AttendanceLimitExceeded>(e => AttendanceCounterScope.KeyFor(e.StudentId, e.AcademicPeriodId));
    }

    public static AttendanceView Create(AttendanceMarked e)
    {
        var view = new AttendanceView
        {
            Id = AttendanceCounterScope.KeyFor(e.StudentId, e.AcademicPeriodId),
            InstitutionId = e.InstitutionId,
            StudentId = e.StudentId,
            AcademicPeriodId = e.AcademicPeriodId,
            BusinessId = e.BusinessId,
            LastUpdated = DateTime.UtcNow
        };

        ApplyAbsenceType(view, e.AbsenceType);
        return view;
    }

    public static void Apply(AttendanceMarked e, AttendanceView view)
    {
        // İşletme sayacın kapsamına girmez ama en son bilinen değer tutulur; eskiden yalnız
        // Create'te yazıldığı için satır ilk işletmede donuyordu (#242).
        view.BusinessId = e.BusinessId;

        ApplyAbsenceType(view, e.AbsenceType);
        view.LastUpdated = DateTime.UtcNow;
    }

    public static void Apply(AttendanceLimitExceeded e, AttendanceView view)
    {
        view.LimitExceeded = true;
        view.LastUpdated = DateTime.UtcNow;
    }

    private static void ApplyAbsenceType(AttendanceView view, string absenceType)
    {
        view.TotalAbsenceDays++;

        // Ayrım politikadan gelir: aynı karar CheckAttendanceLimitHandler'da da veriliyor
        // (henüz yansımamış olayı sayarken). İkisi ayrışırsa sınır yanlış ayaktan tetiklenir (#183).
        if (AttendanceCounterScope.CountsAsUnexcused(absenceType))
            view.UnexcusedDays++;
        else
            view.ExcusedDays++;
    }
}
