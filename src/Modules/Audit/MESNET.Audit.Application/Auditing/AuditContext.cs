using System.Diagnostics;

namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Middleware'in <c>Before</c> → <c>After</c> → <c>Finally</c> hook'ları arasında taşıdığı
/// durum.
/// </summary>
/// <remarks>
/// <para><b>Neden MUTABLE — projede tek istisna:</b> Wolverine'in ürettiği kodda
/// <c>After</c>, başarı yolunda <c>try</c> bloğunun içinde çalışır ve <c>Finally</c> ondan
/// sonra gelir. Bir <i>değer</i> döndürüp aktarmak mümkün değildir: <c>try</c> içinde atanan
/// bir değişken <c>finally</c> bloğunda "kesin atanmış" sayılmaz ve derleme kırılır. Tek
/// mutasyon <see cref="Succeeded"/>'dır ve yalnız <see cref="MarkSucceeded"/> ile yapılır.</para>
///
/// <para><b>Varsayılan başarısızdır.</b> <c>After</c> hiç çalışmazsa (istisna yolu)
/// bayrak <c>false</c> kalır ve <c>Finally</c> hiçbir şey yazmaz — o satırı
/// <c>OnExceptionAsync</c> yazar.</para>
/// </remarks>
public sealed class AuditContext
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    private readonly long _startTimestamp = Stopwatch.GetTimestamp();

    public required Guid ActorId { get; init; }
    public required string ActorName { get; init; }
    public required Type CommandType { get; init; }
    public required object? Command { get; init; }
    public required string? TenantId { get; init; }
    public required Guid? ActorInstitutionId { get; init; }
    public required string? ActorInstitutionPath { get; init; }

    public bool Succeeded { get; private set; }

    public void MarkSucceeded() => Succeeded = true;

    public int ElapsedMs => (int)Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;
}
