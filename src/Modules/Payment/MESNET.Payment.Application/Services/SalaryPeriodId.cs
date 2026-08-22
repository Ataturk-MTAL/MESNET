using System.Security.Cryptography;
using System.Text;

namespace MESNET.Payment.Application.Services;

/// <summary>
/// Maaş dönemi kimliğini (sözleşme, ay) ikilisinden deterministik üretir.
/// </summary>
/// <remarks>
/// Saga kimliği rastgele üretildiği sürece aynı öğrenci/ay için her tetikleme yeni bir ödeme
/// kaydı açıyordu (#62). Deterministik kimlik sayesinde Marten upsert'i aynı satıra yazar ve
/// aynı dönem için ikinci bir ödeme kaydı oluşmaz.
///
/// <para><b>Anahtar (öğrenci, ay) DEĞİL (sözleşme, ay)'dır (#154).</b> Öğrenci anahtarıyla bir
/// öğrenci için ayda tek dönem açılabiliyordu; ay ortasında işletme değiştiğinde iki işverenin
/// ayrı yükümlülüğü tek kayda sıkışıyor, ikinci yerleştirme "zaten var" diye atlanıyordu.
/// Sözleşme zaten istihdam ilişkisinin kendisidir ve fesih → yeni sözleşme zorunlu olduğu için
/// ay ortası her değişim yeni bir sözleşme üretir. (öğrenci, işletme, ay) yetmezdi: aynı
/// işletmeyle ay içinde yeniden sözleşme yapılırsa gene çakışırdı.</para>
///
/// <para>Yan fayda: "zaten var → atla" sınıfı hata (#152) imkânsızlaşır — iki dönemin kimliği
/// farklı olduğu için tüketici sırası önemsizleşir.</para>
/// </remarks>
public static class SalaryPeriodId
{
    /// <param name="contractId">Sözleşme kimliği.</param>
    /// <param name="month">Ay, <c>yyyy-MM</c> formatında (ör. <c>2026-07</c>).</param>
    public static Guid For(Guid contractId, string month)
    {
        // Önek "salary:" olarak KALIYOR ama girdi değişti: eski kimliklerle çakışma olmaz,
        // çünkü hash girdisi farklı. Eski satırlar yetim kalır (dağıtımda temizlenir).
        var bytes = Encoding.UTF8.GetBytes($"salary:{contractId:D}:{month}");
        // MD5 burada kriptografik amaçla değil, sabit 16 baytlık dağılım için kullanılıyor.
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}
