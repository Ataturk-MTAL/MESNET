using System.Security.Cryptography;
using System.Text;

namespace MESNET.Payment.Core.Services;

/// <summary>
/// Sınıf yılı katkı kaydının kimliğini (öğrenci, sınıf yılı) ikilisinden deterministik üretir (#161).
/// </summary>
/// <remarks>
/// Deterministik kimlik sayesinde kayıt Marten upsert'iyle tek satır kalır: aynı sınıf yılının
/// her ayında onay tamamlansa da ikinci kayıt oluşmaz, ilk yazan kalır. Aynı desen maaş
/// döneminde de kullanılıyor (<c>SalaryPeriodId</c>).
/// </remarks>
public static class ContributionClaimId
{
    public static Guid For(Guid studentId, int classYear)
    {
        var bytes = Encoding.UTF8.GetBytes($"contribution-claim:{studentId:D}:{classYear}");
        // MD5 burada kriptografik amaçla değil, sabit 16 baytlık dağılım için kullanılıyor.
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}
