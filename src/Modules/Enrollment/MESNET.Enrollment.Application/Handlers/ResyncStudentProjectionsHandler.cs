using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Shared.Events;
using Wolverine;

namespace MESNET.Enrollment.Application.Handlers;

/// <summary>
/// Öğrenci görünümlerini geçmişe dönük onarır — <b>idempotent</b> (#290).
///
/// <para><b>Onarım olayı yayınlanır, kayıt olayı DEĞİL.</b> <c>StudentRegistered</c>
/// tüketicilerinden biri şube sayacını <b>artırıyor</b>
/// (<c>Coordination.StudentRegisteredCountConsumer</c>) ve görünüm öğrenci başına değil şube
/// başına tek satır. Yeniden yayın her koşuda her şubenin sayacını o şubedeki öğrenci sayısı
/// kadar şişiriyordu; ikinci koşuda sayı ikiye katlanıyordu. Belirti sessizdi: uç 200 döner,
/// log temiz kalır, tek iz yanlış bir sayıdır — ve o sayı
/// <c>UpsertBranchWorkloadConfigHandler</c> üzerinden öğretmen/grup ihtiyacına giriyor.</para>
///
/// <para><b>Sayaç ayrı ve MUTLAK yoldan onarılır:</b> <c>SyncStudentCounts</c> sayacı artırmaz,
/// <c>StudentProfile</c>'dan yeniden hesaplayıp <b>değiştirir</b>.</para>
///
/// <para><b>Neden yalnız sayaç senkronu eklemek yetmezdi:</b>
/// <c>MultipleHandlerBehavior.Separated</c> her handler tipine ayrı kuyruk verir ve kuyruklar
/// arasında sıra garantisi <b>yoktur</b>. "Değiştir" adımı yeniden yayın sürerken çalışsaydı,
/// arkasından gelen artırımlar sayacı yine şişirirdi. Artıranı <b>hiç tetiklememek</b> tek
/// güvenli yoldur; senkron o yüzden düzeltmenin yerine değil, yanına konuldu.</para>
/// </summary>
public static class ResyncStudentProjectionsHandler
{
    public static async Task<ResyncStudentProjectionsResult> Handle(
        ResyncStudentProjections command, IQuerySession session, IMessageBus bus, CancellationToken ct)
    {
        var students = await session.Query<StudentProfile>().ToListAsync(ct);

        // Görünüm besleyen tüketiciler bu olayın aşırı yüklemesini dinler ve idempotent upsert
        // yapar. SmartEnum alanı olay sözleşmesinde string'dir; .Name geçilir (CLAUDE.md).
        foreach (var s in students)
        {
            await bus.PublishAsync(new StudentSnapshotResynced(
                s.Id,
                s.FullName,
                s.InstitutionId,
                s.AcademicPeriodId,
                s.BranchCode,
                s.ClassYear,
                s.EducationType.Name,
                s.StudentNumber ?? "",
                s.HasJourneymanQualification,
                s.BirthDate,
                s.Category.Name,
                // #230 — mevcut öğrencilerin StudentId otoritesi bu resync ile dolar.
                s.KeycloakUserId));
        }

        // Şube sayacı: kurum + dönem başına bir kez, MUTLAK olarak yeniden hesaplanır.
        // InvokeAsync KULLANILMAZ — handler'dan handler'a doğrudan senkron çağrı bu depoda
        // yasaktır; komut yerel dayanıklı kuyruğa konur.
        var scopes = students
            .Select(s => new { s.InstitutionId, s.AcademicPeriodId })
            .Distinct()
            .ToList();

        foreach (var scope in scopes)
            await bus.PublishAsync(new SyncStudentCounts(scope.InstitutionId, scope.AcademicPeriodId));

        return new ResyncStudentProjectionsResult(students.Count, scopes.Count);
    }
}
