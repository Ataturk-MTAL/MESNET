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
    /// <param name="cancellationToken">
    /// <b>KASITLI OLARAK KULLANILMAZ.</b> Bu, isteğin/komutun kendi token'ıdır ve istemci
    /// bağlantıyı koparırsa iptal edilir. "Ayrı oturum" kararının NİYETİ tam da denetim
    /// yazmasını komutun/isteğin ömründen bağımsız kılmaktı (bkz. sınıf yorumu) — bu token'ı
    /// gerçek yazmaya taşımak o niyeti dolaylı yoldan bozar: istemci bağlantıyı keserse iz
    /// sessizce (yalnız log'a) kaybolur. Aşağıda hem kurum yolu araması hem <c>SaveChangesAsync</c>
    /// KASITLI olarak <see cref="CancellationToken.None"/> ile çağrılır.
    /// </param>
    public async Task WriteAsync(
        AuditContext context, Exception? exception, CancellationToken cancellationToken = default)
    {
        try
        {
            var (subjectId, crossed) = AuditEntryFactory.ResolveSubject(
                context.Command, context.ActorInstitutionId);

            // Sıcak yolda EK OKUMA YOK: komutların büyük çoğunluğu aktörün kendi kurumuna
            // yazar ve o dalda yol claim'den gelir. Arama yalnız sınır aşıldığında yapılır.
            // CancellationToken.None: yukarıdaki parametre yorumuna bakın.
            string? subjectPathOverride = crossed && subjectId is { } id
                ? await pathLookup.GetPathAsync(id, CancellationToken.None)
                : null;

            // Id KASITLI olarak context.Id'den geliyor (yeni Guid ÜRETİLMİYOR). Aynı komut
            // için Finally (Succeeded) ve OnException (Failed) ikisi de çağrılırsa (handler
            // döndükten SONRA patlayan hata — transaction commit/cascading publish), aynı
            // kimlikle Marten Store() UPSERT yapar ve son yazan (OnException/Failed) kazanır.
            // Bkz. AuditContext.Id yorumu.
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
            // CancellationToken.None: yukarıdaki parametre yorumuna bakın — istek iptal
            // edilse bile bu yazma DEVAM eder, "ayrı oturum" kararının niyeti budur.
            await session.SaveChangesAsync(CancellationToken.None);
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
