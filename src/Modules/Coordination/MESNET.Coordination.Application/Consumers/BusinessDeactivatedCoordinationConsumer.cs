using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme pasife alındığında coordination view'ı siler.
/// Bu noktada aktif öğrenci ve öğretmen ataması olmadığı garanti edilmiştir (handler validation).
/// </summary>
public static class BusinessDeactivatedCoordinationConsumer
{
    public static void Consume(
        BusinessDeactivated @event,
        IDocumentSession session)
    {
        session.Delete<BusinessCoordinationView>(@event.BusinessId);
    }
}
