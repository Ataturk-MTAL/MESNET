using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Core.Services;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;

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
        RequestPaidLeave command, IDocumentSession session, ICurrentUserService currentUser)
    {
        var studentId = ResolveStudentScope(command, currentUser);

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
        var student = await session.LoadAsync<StudentNameView>(studentId)
            ?? throw new DomainException(AttendanceErrors.StudentNotFound(studentId));

        if (!AbsenceTypePolicy.IsValidForEducationType(AbsenceType.PaidLeave, student.EducationType))
            throw new DomainException(
                AttendanceErrors.PaidLeaveNotAllowedForEducationType(student.EducationType));

        // Onayın 1. adımı işletmededir; işletmesiz yerleştirmede (okulda staj, #159) onaylayacak
        // taraf yoktur. O öğrencinin ücreti de yoktur — ücretli izin konusu doğmaz.
        var placement = await session.Query<InternshipPlacementView>()
            .FirstOrDefaultAsync(p => p.StudentId == studentId
                && p.AcademicPeriodId == command.AcademicPeriodId
                && p.BusinessId != null);

        if (placement?.BusinessId is not { } businessId)
            throw new DomainException(AttendanceErrors.PaidLeavePlacementNotFound(studentId));

        await EnsureNoOverlappingRequestAsync(session, studentId, command);

        var requestId = Guid.NewGuid();
        var @event = new PaidLeaveRequested(
            requestId,
            studentId,
            businessId,
            placement.InstitutionId,
            command.AcademicPeriodId,
            command.StartDate.Date,
            command.EndDate.Date,
            command.Reason.Trim(),
            currentUser.GetUserId(),
            DateTime.UtcNow);

        session.Events.StartStream<PaidLeaveRequest>(requestId, @event);

        return (requestId, @event);
    }

    /// <summary>
    /// Başvurunun hangi öğrenci adına açıldığını çözer (#174).
    ///
    /// <para><b>Öğrenci kendi adına:</b> <c>student_id</c> claim'i kullanılır ve istekte gelen
    /// değer YOK SAYILIR — öğrenci başkası adına başvuramaz.</para>
    ///
    /// <para><b>Veli öğrencisi adına:</b> velinin <c>student_id</c> claim'i yoktur; öğrenci
    /// istekte gelir ama <b>bağ kaydına karşı doğrulanır</b>. Bu, "kapsam istekten alınmaz"
    /// kuralının ihlali değildir: karar isteğe değil sunucudaki otoriter kayda
    /// (<c>UserAccount.LinkedStudentIds</c>) dayanır — <c>branch_codes</c> ile aynı mantık.</para>
    ///
    /// <para>Bağı da claim'i de olmayan çağıran reddedilir.</para>
    /// </summary>
    private static Guid ResolveStudentScope(RequestPaidLeave command, ICurrentUserService currentUser)
    {
        var ownStudentId = currentUser.GetCurrentUser()?.StudentId ?? Guid.Empty;
        if (ownStudentId != Guid.Empty)
            return ownStudentId;

        var requestedStudentId = command.StudentId;
        if (requestedStudentId == Guid.Empty)
            throw new DomainException(AttendanceErrors.PaidLeaveStudentScopeMissing());

        if (!ParentScopePolicy.CanAccessStudent(currentUser.GetLinkedStudentIds(), requestedStudentId))
            throw new DomainException(AttendanceErrors.PaidLeaveStudentScopeMissing());

        return requestedStudentId;
    }

    /// <summary>
    /// Aynı öğrenci için çakışan açık/onaylanmış başvuru var mı. Çakışan iki başvuru
    /// onaylanırsa aynı güne iki izin kaydı açılırdı.
    /// </summary>
    private static async Task EnsureNoOverlappingRequestAsync(
        IQuerySession session, Guid studentId, RequestPaidLeave command)
    {
        // Reddedilenler çakışma saymaz — öğrenci reddedilen aralık için yeniden başvurabilir.
        var openRequests = await session.Query<PaidLeaveRequest>()
            .Where(r => r.StudentId == studentId
                && r.StatusName != nameof(PaidLeaveStatus.Rejected))
            .ToListAsync();

        var overlaps = openRequests.Any(r => PaidLeaveApprovalPolicy.Overlaps(
            r.StartDate, r.EndDate, command.StartDate, command.EndDate));

        if (overlaps)
            throw new DomainException(AttendanceErrors.PaidLeaveOverlappingRequest());
    }
}
