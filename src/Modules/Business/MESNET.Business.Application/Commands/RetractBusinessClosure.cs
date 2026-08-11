namespace MESNET.Business.Application.Commands;

/// <summary>
/// Okul kendi kapatma bildirimini geri çeker (#151). Yeter sayı bildirimlerden hesaplandığı
/// için geri çekme sayıyı düşürür; eşiğin altına inerse işletme <b>kendiliğinden</b> açılır.
///
/// <para>Hedef okul istekten ALINMAZ: aktörün kurum claim'inden gelir ve bir okul yalnız kendi
/// bildirimini geri çekebilir — başka okulunkini kaldırabilseydi yeter sayı anlamsızlaşırdı.</para>
/// </summary>
public sealed record RetractBusinessClosure(Guid BusinessId, string? Reason = null);
