namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Bir öğrencinin belirli bir sınıf yılı için devlet katkısını aldığının kaydı (#161).
/// </summary>
/// <remarks>
/// <para><b>Kural:</b> öğrenci belirli bir sınıf yılı için katkıyı bir kez alır; o sınıf yılı
/// tekrar edildiğinde katkı hesaplanmaz. Katkı alınmamış bir sınıf yılına terfi edildiğinde
/// yeniden işler.</para>
///
/// <para><b>"Tekrar" ayrıca modellenmez</b> — sınıf yılı ikinci kez görüldüğünde kayıt zaten
/// vardır. Bakımı yapılan fazladan bir bayrak eklenmez (#152'nin dersi).</para>
///
/// <para><b>Neden akademik dönem de tutulur:</b> katkı AYLIK hesaplanır, sınıf yılı 9–10 ay
/// sürer. Yalnız "kayıt var mı" diye bakılsaydı, Ekim'de katkı alan öğrenci Kasım ayında
/// kendi ilk yılında bloke olurdu — hiç sınıfta kalmadan katkısını kaybeder ve fatura
/// işletmeye çıkardı. Bloke kararı bu yüzden <see cref="ClassYearContributionPolicy"/>'dedir:
/// aynı akademik dönem = normal ay, farklı akademik dönem + aynı sınıf yılı = tekrar.</para>
/// </remarks>
public class ClassYearContributionClaim
{
    /// <summary>
    /// (öğrenci, sınıf yılı) ikilisinden deterministik türetilir —
    /// <see cref="MESNET.Payment.Core.Services.ContributionClaimId"/>.
    /// </summary>
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    /// <summary>Katkının alındığı sınıf yılı (9–12).</summary>
    public int ClassYear { get; set; }

    /// <summary>
    /// Katkının İLK alındığı akademik dönem. Bloke kararının ekseni budur: aynı dönemdeki
    /// sonraki aylar normal işler, sonraki bir dönemde aynı sınıf yılı görülürse tekrardır.
    /// </summary>
    public Guid FirstAcademicPeriodId { get; set; }

    /// <summary>İlk katkının alındığı maaş ayı (<c>yyyy-MM</c>) — denetim izi.</summary>
    public string FirstClaimedMonth { get; set; } = "";

    /// <summary>Kaydın oluştuğu an — onay zinciri tamamlandığında yazılır.</summary>
    public DateTime ClaimedAt { get; set; }
}
