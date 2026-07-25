using System.Security.Cryptography;
using System.Text;

namespace MESNET.Payment.Application.Services;

/// <summary>
/// Maaş dönemi kimliğini (öğrenci, ay) ikilisinden deterministik üretir.
/// </summary>
/// <remarks>
/// Saga kimliği rastgele üretildiği sürece aynı öğrenci/ay için her tetikleme yeni bir ödeme
/// kaydı açıyordu (#62). Deterministik kimlik sayesinde Marten upsert'i aynı satıra yazar ve
/// aynı ay için ikinci bir ödeme kaydı oluşmaz.
/// </remarks>
public static class SalaryPeriodId
{
    /// <param name="studentId">Öğrenci kimliği.</param>
    /// <param name="month">Ay, <c>yyyy-MM</c> formatında (ör. <c>2026-07</c>).</param>
    public static Guid For(Guid studentId, string month)
    {
        var bytes = Encoding.UTF8.GetBytes($"salary:{studentId:D}:{month}");
        // MD5 burada kriptografik amaçla değil, sabit 16 baytlık dağılım için kullanılıyor.
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}
