using Marten;
using MESNET.Contract.Shared.Events;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Contract olaylarından Payment'ın yerel sözleşme kaydını besler (#154).
/// Aylık maaş dönemlerinin çalışma listesi ve gün oranlamasının kaynağı budur.
/// </summary>
/// <remarks>
/// Bu tüketici <c>ContractWageConsumer</c>'ın yerine geçti. Eski hâli yalnız ücreti taşıyor ve
/// kaydı <c>StudentId</c> ile anahtarlıyordu — öğrenci başına tek sözleşme. Ay içinde işletme
/// değiştiren öğrencide eski sözleşmenin ücreti kayboluyordu (#84 kaydı, #154 ile genişletildi).
/// </remarks>
public static class ContractEmploymentConsumer
{
    /// <remarks>
    /// Kayıt körlemesine yazılmaz: olaylar sıra dışı gelebilir ve <c>ContractActivated</c> ya da
    /// fesih daha önce işlenmiş olabilir. Körlemesine <c>Store</c>, aktifleşmiş bir sözleşmeyi
    /// taslağa ya da feshedilmiş bir sözleşmeyi açığa çevirirdi — ilki maaşı hiç açmaz, ikincisi
    /// biten sözleşmeye ay sonuna kadar ücret yazar.
    /// </remarks>
    public static async Task Consume(ContractCreated @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<ContractEmploymentView>(@event.ContractId)
                   ?? new ContractEmploymentView { Id = @event.ContractId };

        view.StudentId = @event.StudentId;
        view.BusinessId = @event.BusinessId;
        view.InstitutionId = @event.InstitutionId;
        view.AcademicPeriodId = @event.AcademicPeriodId;
        view.StartDate = @event.StartDate;
        view.AgreedMonthlyWage = @event.AgreedMonthlyWage;
        view.LastUpdated = DateTime.UtcNow;

        session.Store(view);
    }

    /// <summary>
    /// Sözleşme aktifleşti — istihdam başladı, maaş dönemi açılabilir.
    /// </summary>
    /// <remarks>
    /// Olay sırası garanti değil: <c>ContractActivated</c> <c>ContractCreated</c>'dan önce
    /// işlenirse kayıt henüz yoktur. O durumda sessizce çıkmak sözleşmeyi kalıcı olarak
    /// taslak gösterir ve maaş hiç açılmaz — bu yüzden asgari kayıt burada da kurulur.
    /// Eksik alanlar <c>ContractCreated</c> geldiğinde dolar; o tüketici <c>IsActivated</c>'ı
    /// ezmemek için mevcut kaydı yükler.
    /// </remarks>
    public static async Task Consume(ContractActivated @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<ContractEmploymentView>(@event.ContractId);

        if (view is null)
        {
            view = new ContractEmploymentView
            {
                Id = @event.ContractId,
                StudentId = @event.StudentId,
                BusinessId = @event.BusinessId,
                StartDate = @event.ActivatedAt
            };
        }

        view.IsActivated = true;
        view.LastUpdated = DateTime.UtcNow;
        session.Store(view);
    }

    /// <summary>
    /// Fesih — istihdam penceresinin üst ucu. Kayıt SİLİNMEZ ve listeden düşmez: ay ortasında
    /// feshedilen sözleşme o ayda çalışılan günlerin ücretini hâlâ hak eder (#154).
    /// </summary>
    public static Task Consume(ContractTerminated @event, IDocumentSession session)
        => CloseAsync(session, @event.ContractId, @event.EndDate);

    public static Task Consume(ContractCompleted @event, IDocumentSession session)
        => CloseAsync(session, @event.ContractId, @event.EndDate);

    private static async Task CloseAsync(IDocumentSession session, Guid contractId, DateTime endDate)
    {
        var view = await session.LoadAsync<ContractEmploymentView>(contractId);
        if (view is null) return;

        view.EndDate = endDate;
        view.LastUpdated = DateTime.UtcNow;
        session.Store(view);
    }
}
