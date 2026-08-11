namespace MESNET.Business.Core.ValueObjects;

/// <summary>
/// Bir okulun "bu işletme kapandı" bildirimi (#151).
///
/// <para><b>Bildirim okula aittir, kullanıcıya değil.</b> Yeter sayı <b>farklı kurum</b>
/// üzerinden sayılır: aynı okuldan iki yetkili sayılsaydı tek okul kendi başına küresel
/// kapatma yapabilir ve kuralın amacı boşa çıkardı. <see cref="ReportedById"/> yalnız
/// denetim izidir, sayıma girmez.</para>
///
/// <para>Bildirim <b>geri çekilebilir</b>; yeter sayı bildirimlerden hesaplandığı için geri
/// çekme sayıyı düşürür ve eşiğin altına inerse işletme kendiliğinden açılır.</para>
/// </summary>
public sealed class BusinessClosureReport
{
    /// <summary>Bildirimi yapan okul — <b>yeter sayının sayım birimi budur</b>.</summary>
    public Guid InstitutionId { get; set; }

    /// <summary>Denetim izi: bildirimi giren kullanıcı. Sayıma girmez.</summary>
    public Guid ReportedById { get; set; }

    public string ReportedByName { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
}
