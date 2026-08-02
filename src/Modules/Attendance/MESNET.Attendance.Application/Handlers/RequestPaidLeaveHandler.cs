using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Core.Services;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Ücretli izin başvurusu açar (#177) — zincirin başlangıcı, öğrenci adımı.
///
/// <para>Bu adım hüküm doğurmaz: devamsızlık kaydı açılmaz, ücrete etki etmez. Başvurunun
/// kendisi de bir <b>ön koşul</b> kümesinden geçer; MESEM olmayan öğrenci başvuru bile açamaz
/// (#175 kuralı burada da uygulanır).</para>
/// </summary>
public static class RequestPaidLeaveHandler
{
    public static async Task<(Guid, PaidLeaveRequested)> Handle(
        RequestPaidLeave command, IDocumentSession session)
    {
        // Kapsam: StudentId token'daki student_id claim'inden gelir (uçta doldurulur).
        // Boşsa istek öğrenci hesabından gelmiyordur — istekten öğrenci seçtirmeyiz.
        if (command.StudentId == Guid.Empty)
            throw new DomainException(AttendanceErrors.PaidLeaveStudentScopeMissing());

        if (!PaidLeaveApprovalPolicy.IsRangeValid(command.StartDate, command.EndDate))
            throw new DomainException(
                AttendanceErrors.PaidLeaveInvalidRange(PaidLeaveApprovalPolicy.MaxLeaveDays));

        // Ücretli izin önceden planlanır — geriye dönük başvuru, geçmiş günlerin ücretini
        // sonradan düzeltmek için kullanılabilirdi.
        if (command.StartDate.Date < DateTime.UtcNow.Date)
            throw new DomainException(AttendanceErrors.PaidLeaveStartsInPast());

        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new DomainException(AttendanceErrors.PaidLeaveReasonRequired());

        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId)
            ?? throw new DomainException(AttendanceErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive)
            throw new DomainException(AttendanceErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        // Ön koşul (#175): ücretli izin hakkı yalnız MESEM öğrencisindedir. Eğitim türü
        // bilinmiyorsa REDDEDİLİR — eksik veri sessizce para sonucu doğurmasın.
        var student = await session.LoadAsync<StudentNameView>(command.StudentId)
            ?? throw new DomainException(AttendanceErrors.StudentNotFound(command.StudentId));

        if (!AbsenceTypePolicy.IsValidForEducationType(AbsenceType.PaidLeave, student.EducationType))
            throw new DomainException(
                AttendanceErrors.PaidLeaveNotAllowedForEducationType(student.EducationType));

        // Onayın 1. adımı işletmededir; işletmesiz yerleştirmede (okulda staj, #159) onaylayacak
        // taraf yoktur. O öğrencinin ücreti de yoktur — ücretli izin konusu doğmaz.
        var placement = await session.Query<InternshipPlacementView>()
            .FirstOrDefaultAsync(p => p.StudentId == command.StudentId
                && p.AcademicPeriodId == command.AcademicPeriodId
                && p.BusinessId != null);

        if (placement?.BusinessId is not { } businessId)
            throw new DomainException(AttendanceErrors.PaidLeavePlacementNotFound(command.StudentId));

        await EnsureNoOverlappingRequestAsync(session, command);

        var requestId = Guid.NewGuid();
        var @event = new PaidLeaveRequested(
            requestId,
            command.StudentId,
            businessId,
            placement.InstitutionId,
            command.AcademicPeriodId,
            command.StartDate.Date,
            command.EndDate.Date,
            command.Reason.Trim(),
            command.StudentId,
            DateTime.UtcNow);

        session.Events.StartStream<PaidLeaveRequest>(requestId, @event);

        return (requestId, @event);
    }

    /// <summary>
    /// Aynı öğrenci için çakışan açık/onaylanmış başvuru var mı. Çakışan iki başvuru
    /// onaylanırsa aynı güne iki izin kaydı açılırdı.
    /// </summary>
    private static async Task EnsureNoOverlappingRequestAsync(
        IQuerySession session, RequestPaidLeave command)
    {
        // Reddedilenler çakışma saymaz — öğrenci reddedilen aralık için yeniden başvurabilir.
        var openRequests = await session.Query<PaidLeaveRequest>()
            .Where(r => r.StudentId == command.StudentId
                && r.StatusName != nameof(PaidLeaveStatus.Rejected))
            .ToListAsync();

        var overlaps = openRequests.Any(r => PaidLeaveApprovalPolicy.Overlaps(
            r.StartDate, r.EndDate, command.StartDate, command.EndDate));

        if (overlaps)
            throw new DomainException(AttendanceErrors.PaidLeaveOverlappingRequest());
    }
}
