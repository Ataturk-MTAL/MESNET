namespace MESNET.Business.Application.Commands;

/// <summary>
/// Okulun "bu işletme kapandı" <b>bildirimi</b> (#151). Tek başına kapatmaz: işletme ancak
/// <b>farklı okullardan</b> gelen bildirim sayısı yeter sayıya ulaşınca küresel olarak kapanır
/// (<c>BusinessClosurePolicy</c>).
///
/// <para>Bildirimi yapan okul istekten ALINMAZ — aktörün kurum claim'inden gelir.</para>
/// </summary>
public sealed record CloseBusiness(Guid BusinessId, string? Reason = null);
