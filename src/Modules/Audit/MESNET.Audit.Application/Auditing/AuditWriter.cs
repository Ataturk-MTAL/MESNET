using Marten;
using MESNET.Audit.Core.Services;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace MESNET.Audit.Application.Auditing;

/// <inheritdoc />
/// <remarks>
/// <para><b>EN KRİTİK KARAR — ayrı oturum.</b> Reddedilen bir komut <c>DomainException</c>
/// atar ve Wolverine'in <c>AutoApplyTransactions()</c> politikası işlemi geri alır. Denetim
/// satırı aynı oturumda yazılsaydı <b>ret kaydı da geri alınırdı</b> — yani en çok istediğimiz
/// satır ("kim neye erişmeye çalıştı") tam da kaydedilmediği an olurdu.</para>
///
/// <para><b>Bedeli: iz en-iyi-çabadır.</b> Denetim yazması patlarsa iş akışı DURMAZ; hata
/// loglanır ve devam edilir. Aksi hâlde bozuk bir denetim tablosu bütün okulu kilitlerdi.
/// Garantili iz bloklayıcı bir tasarım ister; bir okul sisteminde erişilebilirliğin kazanması
/// gerektiği kanısıyla bu seçildi ve BİLİNÇLİDİR.</para>
/// </remarks>
public sealed class AuditWriter(
    IDocumentStore store,
    IInstitutionPathLookup pathLookup,
    ILogger<AuditWriter> logger) : IAuditWriter
{
    public async Task WriteAsync(
        AuditContext context, Exception? exception, CancellationToken cancellationToken = default)
    {
        try
        {
            var (subjectId, crossed) = AuditEntryFactory.ResolveSubject(
                context.Command, context.ActorInstitutionId);

            // Sıcak yolda EK OKUMA YOK: komutların büyük çoğunluğu aktörün kendi kurumuna
            // yazar ve o dalda yol claim'den gelir. Arama yalnız sınır aşıldığında yapılır.
            string? subjectPathOverride = crossed && subjectId is { } id
                ? await pathLookup.GetPathAsync(id, cancellationToken)
                : null;

            var input = new AuditInput(
                Id: context.Id,
                OccurredAt: context.OccurredAt,
                ActorId: context.ActorId,
                ActorName: context.ActorName,
                CommandType: context.CommandType,
                Command: context.Command,
                TenantId: context.TenantId,
                ActorInstitutionId: context.ActorInstitutionId,
                ActorInstitutionPath: context.ActorInstitutionPath,
                SubjectInstitutionPathOverride: subjectPathOverride,
                DurationMs: context.ElapsedMs);

            var entry = exception is null
                ? AuditEntryFactory.Succeeded(input)
                : AuditEntryFactory.Failed(input, exception);

            // Kiracı AÇIKÇA verilir. Argümansız session yasaktır
            // (DefaultTenantUsageEnabled = false) ve kiracısız yazma istisnaya döner —
            // yani iz kaybolurdu. Kurum üstü işler platform kiracısına düşer.
            var tenantId = string.IsNullOrEmpty(context.TenantId)
                ? TenantResolution.Platform
                : context.TenantId;

            await using var session = store.LightweightSession(tenantId);
            session.Store(entry);
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // İZ EN-İYİ-ÇABADIR (bkz. sınıf yorumu). Burada fırlatmak, denetim tablosundaki
            // bir arızayı bütün okulun iş akışına yayardı.
            logger.LogError(ex,
                "Denetim satırı yazılamadı — Komut: {CommandType}, Aktör: {ActorId}",
                context.CommandType.Name, context.ActorId);
        }
    }
}
