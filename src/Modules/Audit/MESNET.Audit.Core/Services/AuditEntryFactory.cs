using MESNET.Audit.Core.Entities;
using MESNET.Audit.Core.Enums;
using MESNET.Common.Shared;

namespace MESNET.Audit.Core.Services;

/// <summary>
/// Denetim satırının kurulması için gereken girdilerin tamamı. Middleware yalnız bunları
/// toplar; karar <see cref="AuditEntryFactory"/>'dedir.
/// </summary>
/// <param name="SubjectInstitutionPathOverride">
/// Konu kurum aktörün kurumundan farklıysa dışarıdan çözülen yol. Çözülemediyse
/// <c>null</c> — satır yine yazılır.
/// </param>
public sealed record AuditInput(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string ActorName,
    Type CommandType,
    object? Command,
    string? TenantId,
    Guid? ActorInstitutionId,
    string? ActorInstitutionPath,
    string? SubjectInstitutionPathOverride,
    int DurationMs,
    Guid? ActiveInstitutionId = null);

/// <summary>
/// Denetim satırının içeriğine karar veren SAF fonksiyon.
/// </summary>
/// <remarks>
/// Karar burada, girdi toplama middleware'de — <c>InstitutionScopePolicy</c> /
/// <c>InstitutionScopeGuardMiddleware</c> ile aynı ayrım. Böylece sonuç eşlemesi ve kiracı
/// sınırı hesabı canlı bir Wolverine ana bilgisayarı olmadan test edilebilir.
/// </remarks>
public static class AuditEntryFactory
{
    private const string InstitutionTargetName = "InstitutionId";

    public static AuditEntry Succeeded(AuditInput input)
        => Build(input, AuditOutcome.Succeeded, errorCode: null);

    /// <summary>
    /// Başarısız komutun satırı.
    ///
    /// <para><b><see cref="DomainException"/> → <see cref="AuditOutcome.Rejected"/>:</b>
    /// "sistem çalıştı, kural izin vermedi". Kurum kapsamı ihlali de buradadır — o guard
    /// middleware'de çalışır ve <c>DomainException</c> fırlatır.</para>
    ///
    /// <para><b>Diğer istisna → <see cref="AuditOutcome.Failed"/>:</b> "sistem çalışmadı".
    /// Saklanan tek şey istisna TİPİNİN adıdır; mesaj PII taşıyabilir.</para>
    /// </summary>
    public static AuditEntry Failed(AuditInput input, Exception exception)
        => exception is DomainException domain
            ? Build(input, AuditOutcome.Rejected, domain.Error.Code)
            : Build(input, AuditOutcome.Failed, exception.GetType().Name);

    /// <summary>
    /// Konu kurumu ve kiracı sınırının aşılıp aşılmadığını çözer.
    /// </summary>
    /// <remarks>
    /// <b>Neden ayrıca public:</b> denetim yazıcısı, yolu aramaya gerek olup olmadığını
    /// satırı kurmadan ÖNCE bilmek zorundadır (arama bir veritabanı gidişidir ve komutların
    /// büyük çoğunluğu kendi kurumuna yazar). Yazıcı bu kararı kendi kopyalasaydı iki yerde
    /// iki ayrı "konu kurum" tanımı doğardı.
    /// </remarks>
    public static (Guid? SubjectInstitutionId, bool CrossedTenantBoundary) ResolveSubject(
        object? command, Guid? actorInstitutionId, Guid? activeInstitutionId)
    {
        var targets = AuditTargetExtractor.Extract(command);

        // Konu kurum: komut bir kurumu HEDEFLİYORSA o; aksi hâlde aktif bağlam VARSA o;
        // aksi hâlde aktörün (ev) kurumu. IInstitutionScoped arayüzüne bakılmaz — o tip
        // Institution.Application'dadır ve Audit hiçbir modülü referans etmez (Görev 1,
        // Step 4).
        //
        // AKTİF BAĞLAM EV KURUMUNDAN ÖNCE GELİR (B parçası). Gelmeseydi, il yetkilisinin
        // okulda yaptığı hedefsiz her yazma ize İL adına düşerdi ve CrossedTenantBoundary
        // her zaman false olurdu — yani "il yetkilisi hangi okula dokundu" sorusu, B'nin
        // izli verilmesinin TEK sebebi, cevapsız kalırdı.
        var subjectInstitutionId =
            targets.TryGetValue(InstitutionTargetName, out var targeted) ? targeted
            : activeInstitutionId is { } active && active != Guid.Empty ? active
            : actorInstitutionId;

        // Sınır aşımı bir İDDİADIR; veri eksikliği onu doğurmaz. Kurumsuz aktörde
        // karşılaştıracak taraf yoktur, o yüzden false.
        var crossed = actorInstitutionId is { } actorInstitution
                      && subjectInstitutionId is { } subject
                      && actorInstitution != subject;

        return (subjectInstitutionId, crossed);
    }

    private static AuditEntry Build(AuditInput input, AuditOutcome outcome, string? errorCode)
    {
        var targets = AuditTargetExtractor.Extract(input.Command);
        var (commandType, module) = AuditCommandDescriptor.Describe(input.CommandType);

        var (subjectInstitutionId, crossed) = ResolveSubject(
            input.Command, input.ActorInstitutionId, input.ActiveInstitutionId);

        // Sıcak yolda EK OKUMA YOK: konu aktörün kendi kurumuysa yol claim'den gelir.
        var subjectPath = crossed
            ? input.SubjectInstitutionPathOverride
            : input.ActorInstitutionPath;

        return new AuditEntry
        {
            Id = input.Id,
            OccurredAt = input.OccurredAt,
            ActorId = input.ActorId,
            ActorName = input.ActorName,
            CommandType = commandType,
            CommandLabel = AuditCommandLabels.For(commandType),
            Module = module,
            TenantId = input.TenantId,
            ActorInstitutionId = input.ActorInstitutionId,
            SubjectInstitutionId = subjectInstitutionId,
            SubjectInstitutionPath = subjectPath,
            CrossedTenantBoundary = crossed,
            OutcomeName = outcome.Name,
            ErrorCode = errorCode,
            TargetIds = targets,
            DurationMs = input.DurationMs,
        };
    }
}
