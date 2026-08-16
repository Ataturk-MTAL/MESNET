using System.Security.Cryptography;
using System.Text;

namespace MESNET.Internship.Core.Services;

/// <summary>
/// Staj saga'sının kimliği <b>yerleştirmeden deterministik</b> üretilir (#251).
///
/// <para><b>Yaşanan:</b> <c>Start(StudentPlaced)</c> her olayda <c>Guid.NewGuid()</c> çağırıyordu.
/// Aynı <c>StudentPlaced</c> tekrar yayınlandığında (seeder tekrar çalıştı, olay yeniden
/// oynatıldı) <b>yeni bir saga daha</b> doğuyordu. Ölçüldü: <b>2248 saga, yalnız 95 farklı
/// yerleştirme</b> — yerleştirme başına ~24 kopya.</para>
///
/// <para><b>Neden zararlı:</b> aktarıcı (bkz. <c>SagaRelayConsumer</c>) adaylardan birini seçmek
/// zorunda kalır ve kalan kopyalar hayalet olur. Canlı ölçümde tam olarak bu görüldü: devamsızlık
/// sınırı aşılınca <b>bir</b> saga <c>TerminationInProgress</c>'e geçti, aynı öğrencinin diğer 23
/// saga'sı <c>AwaitingContract</c>'ta kaldı. Yani fesih zinciri düzeltildikten sonra bile sonuç
/// tutarsız kalıyordu.</para>
///
/// <para>Deterministik kimlikle tekrar gelen olay <b>aynı satıra</b> düşer; ikinci saga doğmaz.
/// Aynı desen <c>PaymentSaga</c>'da (sözleşme, ay) ikilisiyle ve
/// <c>ContributionClaimId</c>'de (öğrenci, sınıf yılı) ikilisiyle zaten kullanılıyor.</para>
///
/// <para><b>Anahtar neden yerleştirme:</b> staj bir yerleştirmedir. Öğrenci fesih sonrası yeni
/// işletmeye yerleşirse yeni bir <c>PlacementId</c> doğar ve <b>yeni bir saga</b> gerekir —
/// öğrenci+dönem anahtarı olsaydı ikinci staj birincinin üzerine yazardı.</para>
/// </summary>
public static class InternshipSagaId
{
    public static Guid For(Guid placementId)
    {
        var bytes = Encoding.UTF8.GetBytes($"internship-saga:{placementId:D}");
        // MD5 kriptografik amaçla değil, sabit 16 baytlık dağılım için.
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}
