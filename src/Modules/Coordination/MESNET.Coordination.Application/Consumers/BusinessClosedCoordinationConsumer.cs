using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme kapatıldığında coordination view'ı kalıcı olarak siler.
/// Bu noktada aktif öğrenci ve öğretmen ataması olmadığı garanti edilmiştir (handler validation).
/// </summary>
public static class BusinessClosedCoordinationConsumer
{
    public static void Consume(
        BusinessClosed @event,
        IDocumentSession session)
    {
        session.Delete<BusinessCoordinationView>(@event.BusinessId);
    }
}
