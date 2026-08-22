using Marten;
using MESNET.Enrollment.Shared.Events;
using MESNET.Security.Core.ReadModels;

namespace MESNET.Security.Application.Consumers;

/// <summary>
/// Öğrenci kaydından Security'nin yerel görünümünü besler (#271) — veli bağı eksiklerini
/// ölçebilmek için.
///
/// <para>Aynı olayı <c>StudentAccountSyncConsumer</c> de tüketiyor ama o
/// <c>UserAccount.StudentId</c> otoritesini dolduruyor (#230); bu görünüm ayrı bir soruyu
/// yanıtlıyor: <b>hangi öğrencinin velisi yok</b>.</para>
///
/// <para><b>İdempotenttir</b> — <c>Store</c> upsert yapar, olay yeniden yayınlanabilir
/// (<c>POST /api/students/resync-projections</c>).</para>
/// </summary>
public static class GuardianLinkViewConsumer
{
    public static void Consume(StudentRegistered @event, IDocumentSession session)
    {
        session.Store(new GuardianLinkView
        {
            Id = @event.StudentId,
            InstitutionId = @event.InstitutionId,
            FullName = @event.FullName,
            StudentNumber = string.IsNullOrWhiteSpace(@event.StudentNumber) ? null : @event.StudentNumber,
            BranchCode = @event.BranchCode
        });
    }
}
