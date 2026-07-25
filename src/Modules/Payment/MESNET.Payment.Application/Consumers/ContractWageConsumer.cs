using Marten;
using MESNET.Contract.Shared.Events;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Sözleşmede taahhüt edilen aylık ücreti Payment modülüne taşır (#84).
/// Maaş hesabında yasal taban ALT SINIR olarak kullanılır; sözleşme ücreti daha yüksekse
/// esas alınan odur (3308 Madde 25).
/// </summary>
public static class ContractWageConsumer
{
    public static void Consume(ContractCreated @event, IDocumentSession session)
    {
        session.Store(new StudentContractWageView
        {
            Id = @event.StudentId,
            ContractId = @event.ContractId,
            BusinessId = @event.BusinessId,
            AgreedMonthlyWage = @event.AgreedMonthlyWage,
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        });
    }

    // Sözleşme kapandığında ücret taahhüdü de düşer. Kayıt silinmiyor; hangi sözleşmeden
    // geldiği ve ne zaman kapandığı görünür kalsın.
    public static async Task Consume(ContractTerminated @event, IDocumentSession session)
        => await DeactivateAsync(@event.StudentId, @event.ContractId, session);

    public static async Task Consume(ContractCompleted @event, IDocumentSession session)
        => await DeactivateAsync(@event.StudentId, @event.ContractId, session);

    private static async Task DeactivateAsync(Guid studentId, Guid contractId, IDocumentSession session)
    {
        var view = await session.LoadAsync<StudentContractWageView>(studentId);

        // Başka (daha yeni) bir sözleşmeye aitse dokunma — fesih sonrası yeni sözleşme akışında
        // olaylar sıra dışı gelebilir.
        if (view is null || view.ContractId != contractId) return;

        view.IsActive = false;
        view.LastUpdated = DateTime.UtcNow;
        session.Store(view);
    }
}
