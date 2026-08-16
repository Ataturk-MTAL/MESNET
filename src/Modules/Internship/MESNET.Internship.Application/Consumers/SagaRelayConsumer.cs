using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Contract.Shared.Events;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Policies;
using MESNET.Internship.Shared.Events;
using Microsoft.Extensions.Logging;

namespace MESNET.Internship.Application.Consumers;

/// <summary>
/// Başka modülün olaylarını saga'ya taşıyan <b>aktarıcılar</b> (#248).
///
/// <para><b>Neden var:</b> Wolverine saga kimliğini mesajın alan ADINDAN çözer. Ölçüldü —
/// Internship'in kendi mesajları (<c>InternshipTerminationRequested</c>,
/// <c>ApproveTerminationBy*</c>) <c>InternshipId</c> taşıdığı için çalışıyordu; başka modülün
/// dört olayı taşımadığı için <c>IndeterminateSagaStateIdException</c> ile ölü mektuba
/// düşüyordu. <b>Hiçbiri hata olarak görünmüyordu.</b></para>
///
/// <para><b>Ölçülen sonuç:</b> 2248 saga'nın hiçbirinde <c>contractId</c> yok, 2139'u sözleşme
/// aktifken bile <c>AwaitingContract</c>'ta çakılı, devamsızlık sınırı hiç fesih başlatmamış,
/// fesih sonrası yeniden yerleştirme ve staj kapanışı hiç tetiklenmemiş.</para>
///
/// <para><b>Neden olaya alan eklenmedi:</b> saga kimliği Internship'in iç bilgisidir; onu
/// <c>Contract.Shared</c>/<c>Attendance.Shared</c>'e koymak modül sınırını deler.
/// Gerekçenin tamamı <see cref="SagaCorrelationPolicy"/>'de.</para>
///
/// <para><b>Saga bulunamazsa</b> mesaj <b>sessizce düşürülmez</b> — uyarı yazılır. Bu, dört
/// olayın da yıllarca sessizce kaybolmasının sebebiydi.</para>
///
/// <para><b>Sınıf adı TEKİL olmak zorunda:</b> Wolverine handler keşfi sınıf adının
/// <c>Handler</c> ya da <c>Consumer</c> ile bitmesini ister. Çoğul <c>Consumers</c> keşfedilmez
/// ve mesaj <i>No routes can be determined</i> ile düşer — ölçüldü, ilk denemede tam olarak bu
/// oldu. Yine hata değil, yine sessizlik.</para>
/// </summary>
public static class SagaRelayConsumer
{
    // ─── Sözleşme aktifleşti → staja bağla ───
    public static async Task<LinkInternshipContract?> Consume(
        ContractActivated @event, IQuerySession session, ILogger logger)
    {
        var sagaId = await FindByContractAsync(session, @event.StudentId, @event.BusinessId);
        if (sagaId is null)
        {
            Warn(logger, nameof(ContractActivated), @event.StudentId, @event.BusinessId);
            return null;
        }

        return new LinkInternshipContract(sagaId.Value, @event.ContractId);
    }

    // ─── Sözleşme feshedildi → stajı feshe kapat ───
    public static async Task<TerminateInternshipContract?> Consume(
        ContractTerminated @event, IQuerySession session, ILogger logger)
    {
        var sagaId = await FindByContractAsync(session, @event.StudentId, @event.BusinessId);
        if (sagaId is null)
        {
            Warn(logger, nameof(ContractTerminated), @event.StudentId, @event.BusinessId);
            return null;
        }

        return new TerminateInternshipContract(sagaId.Value, @event.Reason);
    }

    // ─── Sözleşme tamamlandı → stajı kapat ───
    public static async Task<CompleteInternshipContract?> Consume(
        ContractCompleted @event, IQuerySession session, ILogger logger)
    {
        var sagaId = await FindByContractAsync(session, @event.StudentId, @event.BusinessId);
        if (sagaId is null)
        {
            Warn(logger, nameof(ContractCompleted), @event.StudentId, @event.BusinessId);
            return null;
        }

        return new CompleteInternshipContract(sagaId.Value);
    }

    // ─── Devamsızlık sınırı aşıldı → fesih onay zincirini başlat ───
    //
    // Saga'da ayrı bir Handle(AttendanceLimitExceeded) YOK: manuel fesihle aynı yola girer
    // (InternshipTerminationRequested). O yol dev'de fiilen çalıştığı ölçülmüştü; ikinci bir
    // giriş noktası ikinci bir sessiz kırılma yüzeyi demek olurdu.
    public static async Task<InternshipTerminationRequested?> Consume(
        AttendanceLimitExceeded @event, IQuerySession session, ILogger logger)
    {
        var sagaId = await FindByAttendanceAsync(session, @event.StudentId, @event.AcademicPeriodId);
        if (sagaId is null)
        {
            logger.LogWarning(
                "Devamsızlık sınırı aşıldı ama açık staj saga'sı bulunamadı — fesih başlatılamıyor. "
                + "Öğrenci: {StudentId}, dönem: {AcademicPeriodId}, {Days}/{Limit} gün ({Kind}).",
                @event.StudentId, @event.AcademicPeriodId, @event.TotalAbsenceDays,
                @event.Limit, @event.LimitKind);
            return null;
        }

        var reason = $"Devamsızlık limiti aşıldı: {@event.TotalAbsenceDays}/{@event.Limit} gün";

        return new InternshipTerminationRequested(
            sagaId.Value, reason, "AttendanceLimitExceeded", "Sistem");
    }

    private static async Task<Guid?> FindByContractAsync(
        IQuerySession session, Guid studentId, Guid businessId)
    {
        // Öğrencinin saga'ları çekilir, eşleşme kararı POLİTİKADA verilir: InternshipPhase bir
        // SmartEnum ve Marten LINQ'inde faz karşılaştırması yapılamaz (bkz. CLAUDE.md).
        var candidates = await session.Query<InternshipSaga>()
            .Where(s => s.StudentId == studentId)
            .ToListAsync();

        return candidates
            .FirstOrDefault(s => SagaCorrelationPolicy.MatchesContract(
                s.StudentId, s.BusinessId, s.Phase, studentId, businessId))?.Id;
    }

    private static async Task<Guid?> FindByAttendanceAsync(
        IQuerySession session, Guid studentId, Guid academicPeriodId)
    {
        var candidates = await session.Query<InternshipSaga>()
            .Where(s => s.StudentId == studentId)
            .ToListAsync();

        return candidates
            .FirstOrDefault(s => SagaCorrelationPolicy.MatchesAttendance(
                s.StudentId, s.AcademicPeriodId, s.Phase, studentId, academicPeriodId))?.Id;
    }

    private static void Warn(ILogger logger, string eventName, Guid studentId, Guid businessId)
        => logger.LogWarning(
            "{Event} için açık staj saga'sı bulunamadı — saga güncellenmiyor. "
            + "Öğrenci: {StudentId}, işletme: {BusinessId}.",
            eventName, studentId, businessId);
}
