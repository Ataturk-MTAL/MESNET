using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Core.Services;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;

namespace MESNET.Attendance.Application.Handlers;

public static class MarkAttendanceHandler
{
    public static async Task<(Guid, AttendanceMarked, NotifyAttendancePendingApproval?)> Handle(
        MarkAttendance command, IDocumentSession session, ICurrentUserService currentUser)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(AttendanceErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(AttendanceErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        // Öğrenci-işletme eşleşmesi doğrulaması
        var placement = await session.Query<InternshipPlacementView>()
            .FirstOrDefaultAsync(p => p.StudentId == command.StudentId
                && p.BusinessId == command.BusinessId);

        if (placement is null)
            throw new DomainException("ATTENDANCE_INVALID_PLACEMENT",
                "Bu öğrenci-işletme eşleşmesi bulunamadı. Devamsızlık girişi yapılamaz.");

        // Geçerli hafta kısıtı — MEB e-Okul uyumu: sadece bu hafta için giriş yapılabilir
        var today = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7; // Pazartesi = 0
        var weekStart = today.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(6); // Pazar

        if (command.Date.Date < weekStart || command.Date.Date > weekEnd)
            throw new DomainException("ATTENDANCE_OUTSIDE_CURRENT_WEEK",
                "Devamsızlık girişi sadece geçerli hafta için yapılabilir. Geriye veya ileriye dönük giriş yapılamaz.");

        var calendar = await session.Query<WorkCalendar>()
            .FirstOrDefaultAsync(c => c.InstitutionId == command.InstitutionId && c.Year == command.Date.Year);

        if (calendar?.RestrictedDays.Any(d => d.Date.Date == command.Date.Date) == true)
            throw new DomainException("ATTENDANCE_RESTRICTED_DATE",
                "Bu tarih kısıtlı bir gündür, devamsızlık girişi yapılamaz.");

        if (!AbsenceType.TryFromName(command.AbsenceType, true, out var absenceType))
            throw new DomainException("ATTENDANCE_INVALID_ABSENCE_TYPE",
                $"Geçersiz devamsızlık türü: {command.AbsenceType}.");

        // Aktör kimliği saklanır, adı değil (#139) — ad okuma anında UserNameView'dan çözülür.
        var markedById = currentUser.GetUserId();
        // İşletme tarafından girilen devamsızlık okul onayı bekler.
        //
        // Kontrol #172'ye kadar ROL ADINA bakıyordu (CompanyManager || MasterTrainer) ve
        // CLAUDE.md'de teknik borç olarak yazılıydı. Artık kararı permission veriyor
        // (ADR-0001): `attendance:direct-entry` yalnız okul rollerindedir ve
        // NeverDirectlyAssignable olduğu için bir işletme kullanıcısına bireysel atanamaz.
        // Rol adı listesi olsaydı #172'de eklenen CompanyHR sessizce dışarıda kalır, o rolün
        // girdiği kayıt okul girmiş gibi doğrudan "Recorded" olurdu.
        var hasDirectEntry = currentUser.HasPermission(Permissions.Attendance.DirectEntry);
        var isPendingEntry = !hasDirectEntry;

        // Ücretli izin DOĞRUDAN GİRİLEMEZ (#177) — okul tarafı da giremez. Yalnız öğrenci
        // başvurusunun işletme ve okul onayından geçmesiyle doğar; kayıtları
        // PaidLeaveAttendanceConsumer açar. Kısıt komut yolundadır, olay yolunda değil.
        if (AbsenceTypePolicy.RequiresApprovedRequest(absenceType))
            throw new DomainException(AttendanceErrors.PaidLeaveRequiresApprovedRequest());

        // İşletme resmî izin veremez, yalnız devamsızlık bildirir (#175). Sınıflandırma —
        // mazeret, izin, sağlık raporu — okul tarafındadır ve her biri ücreti etkiler.
        if (!AbsenceTypePolicy.CanReport(absenceType, hasDirectEntry))
            throw new DomainException(AttendanceErrors.TypeNotReportableByBusiness(absenceType.Slug));

        var initialStatus = isPendingEntry
            ? AttendanceStatus.Pending.Name
            : AttendanceStatus.Recorded.Name;

        var id = Guid.NewGuid();
        var @event = new AttendanceMarked(
            id, command.StudentId, command.BusinessId,
            command.InstitutionId, command.AcademicPeriodId,
            command.Date, command.AbsenceType, markedById, initialStatus);

        session.Events.StartStream<AttendanceRecord>(id, @event);

        NotifyAttendancePendingApproval? notification = null;
        if (isPendingEntry && placement.TeacherId is not null)
        {
            notification = new NotifyAttendancePendingApproval(
                id, command.StudentId, command.BusinessId,
                command.InstitutionId, placement.TeacherId.Value,
                markedById, command.Date, command.AbsenceType);
        }

        return (id, @event, notification);
    }
}
