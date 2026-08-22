namespace MESNET.Business.Shared.Events;

/// <summary>
/// Okul kendi kapatma bildirimini geri çekti (#151). <see cref="Reopened"/> doğruysa sayı
/// eşiğin altına indiği için işletme <b>kendiliğinden</b> yeniden açılmıştır.
/// </summary>
public sealed record BusinessClosureRetracted(
    Guid BusinessId,
    Guid InstitutionId,
    int ReportingInstitutionCount,
    bool Reopened);
