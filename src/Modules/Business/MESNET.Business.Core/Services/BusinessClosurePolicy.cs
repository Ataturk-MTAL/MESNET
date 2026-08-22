using MESNET.Business.Core.ValueObjects;

namespace MESNET.Business.Core.Services;

/// <summary>
/// Bir işletmenin küresel olarak kapanıp kapanmayacağı kararı (#151).
///
/// <para><b>Sayım farklı KURUM üzerinden yapılır, farklı kullanıcı üzerinden değil.</b> Aynı
/// okuldan iki yetkili sayılsaydı tek okul kendi başına küresel kapatma yapabilir ve kuralın
/// amacı boşa çıkardı. İşletme kataloğu paylaşımlıdır: bir okulun kararı bütün okulları
/// etkiler, o yüzden karar da birden çok okuldan gelmelidir.</para>
///
/// <para><b>⚠ BİLİNÇLİ ASİMETRİ — kapatma yeter sayı ister, açma istemez.</b> Yeniden açmayı
/// herhangi bir okuldan tek yetkili yapabilir; yani <b>iki okulun kararını tek okul geri
/// alabilir</b>. Bu hata değildir: yanlış kapatılmış bir işletme, yanlış açık kalandan daha
/// zararlıdır — kapalı işletme süzgeçten düşer, öğrenci yerleştirilemez, sözleşme yapılamaz.
/// Sistem <b>açık kalmaya doğru</b> hata yapmalıdır.</para>
///
/// <para><b>Bu paragraf silinirse biri bunu hata olarak açar.</b></para>
/// </summary>
public static class BusinessClosurePolicy
{
    /// <summary>
    /// Faz 1 varsayılanı. Tek okul çalışırken yeter sayı <b>1</b>'dir — davranış #151 öncesiyle
    /// birebir aynı kalır. Çok okullu kuruluma geçerken yapılandırmadan 2'ye çekilir; kod
    /// değişmez.
    /// </summary>
    public const int DefaultQuorum = 1;

    /// <summary>
    /// Bildirimde bulunmuş <b>farklı okul</b> sayısı. Aynı okuldan gelen birden çok bildirim
    /// bir sayılır.
    /// </summary>
    public static int DistinctReportingInstitutions(IEnumerable<BusinessClosureReport> reports) =>
        reports.Select(r => r.InstitutionId).Distinct().Count();

    /// <summary>
    /// Yeter sayıya ulaşıldı mı? Eşik en az 1 kabul edilir: 0 ya da negatif bir yapılandırma
    /// değeri "hiç bildirim olmadan kapalı" anlamına gelirdi ve bütün katalog kapanırdı.
    /// </summary>
    public static bool ReachesQuorum(IEnumerable<BusinessClosureReport> reports, int quorum) =>
        DistinctReportingInstitutions(reports) >= Math.Max(1, quorum);

    /// <summary>
    /// Bir okul <b>yalnız kendi</b> bildirimini geri çekebilir. Başka okulun bildirimini
    /// kaldırabilseydi yeter sayı anlamsızlaşırdı: iki okulun kararını, üçüncü okul tek başına
    /// bozardı.
    /// </summary>
    public static bool CanRetract(BusinessClosureReport report, Guid actorInstitutionId) =>
        actorInstitutionId != Guid.Empty && report.InstitutionId == actorInstitutionId;
}
