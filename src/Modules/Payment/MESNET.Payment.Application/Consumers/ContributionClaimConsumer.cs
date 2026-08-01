using Marten;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Onay zinciri tamamlanan ödemede, o sınıf yılı için devlet katkısının alındığını kaydeder (#161).
/// </summary>
/// <remarks>
/// <para><b>Neden hesap anında değil onay anında:</b> hesap anında yazılsaydı sonradan
/// reddedilen bir ödeme öğrenciyi <b>hiç almadığı</b> bir katkı için bloke ederdi. Kayıt fiilen
/// ödenen katkıyı temsil eder.</para>
///
/// <para><b>Katkı sıfırsa kayıt YAZILMAZ.</b> Kamu kurumunda (#157) ya da zaten blokeli bir ayda
/// katkı doğmaz; o ayı "alınmış" saymak, öğrencinin gerçekten hak ettiği bir sınıf yılını
/// sessizce yakardı.</para>
///
/// <para>Kayıt <b>ilk</b> katkıda oluşur ve bir daha güncellenmez: içindeki akademik dönem,
/// aynı sınıf yılının sonraki aylarını normal işletirken sonraki YILDA aynı sınıfı bloke eden
/// eksendir. Sonraki ayların onayında üzerine yazılsaydı eksen kayar ve bloke hiç çalışmazdı.</para>
/// </remarks>
public static class ContributionClaimConsumer
{
    public static async Task Consume(PaymentCompleted @event, IDocumentSession session)
    {
        // Sınıf yılı bilinmiyorsa kayıt açılmaz — yanlış sınıfa yazmak, öğrencinin gerçek
        // sınıf yılını haksız yere bloke etmek demektir.
        if (@event.ClassYear <= 0) return;

        // Katkı fiilen doğmadıysa sınıf yılı tüketilmemiştir.
        if (@event.GovernmentContribution <= 0m) return;

        var id = ContributionClaimId.For(@event.StudentId, @event.ClassYear);

        var existing = await session.LoadAsync<ClassYearContributionClaim>(id);
        if (existing is not null) return;

        session.Store(new ClassYearContributionClaim
        {
            Id = id,
            StudentId = @event.StudentId,
            ClassYear = @event.ClassYear,
            FirstAcademicPeriodId = @event.AcademicPeriodId,
            FirstClaimedMonth = @event.Month,
            ClaimedAt = DateTime.UtcNow
        });
    }
}
