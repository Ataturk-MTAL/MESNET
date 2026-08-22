using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Core.Services;

/// <summary>
/// Sınıf yılı katkısının tükenip tükenmediğine karar verir (#161). Saf fonksiyon — G/Ç yapmaz.
/// </summary>
/// <remarks>
/// Karar hesaplayıcının DIŞINDA durur: <c>SalaryCalculator</c> saf ve geçmişsizdir, yalnız o ayın
/// girdileriyle çalışır. Geçmişe bakan karar burada toplanır ve hesaplayıcıya <c>bool</c> girdi
/// olarak verilir — #157'deki <c>isPublicInstitution</c> deseninin aynısı.
/// </remarks>
public static class ClassYearContributionPolicy
{
    /// <param name="claim">
    /// Öğrencinin o sınıf yılına ait katkı kaydı; <c>null</c> = bu sınıf yılı için katkı hiç
    /// alınmamış (ilk yıl ya da terfi sonrası yeni sınıf).
    /// </param>
    /// <param name="currentAcademicPeriodId">Hesaplanan maaş döneminin akademik dönemi.</param>
    /// <returns>
    /// Katkı tükenmişse <c>true</c>: kayıt var VE başka bir akademik dönemde açılmış — yani
    /// aynı sınıf yılı ikinci kez okunuyor.
    /// </returns>
    public static bool IsExhausted(ClassYearContributionClaim? claim, Guid currentAcademicPeriodId)
    {
        if (claim is null) return false;

        // Aynı akademik dönem: sınıf yılının normal ayları. Katkı aylık ödenir, sınıf yılı
        // 9–10 ay sürer; burada bloke edilseydi öğrenci ilk yılında ikinci aydan itibaren
        // katkısını kaybederdi.
        return claim.FirstAcademicPeriodId != currentAcademicPeriodId;
    }
}
