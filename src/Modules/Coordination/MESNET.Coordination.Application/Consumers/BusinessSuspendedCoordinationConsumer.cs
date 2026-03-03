using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme suspend edildiğinde coordination view'ı siler.
/// Bu noktada aktif öğrenci ve öğretmen ataması olmadığı garanti edilmiştir (handler validation).
/// </summary>
public static class BusinessSuspendedCoordinationConsumer
{
    public static void Consume(
        BusinessSuspended @event,
        IDocumentSession session)
    {
        session.Delete<BusinessCoordinationView>(@event.BusinessId);
    }
}
