using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme suspend edildiğinde coordination view satırlarını siler (tüm alanlar + temel satır).
/// Bu noktada aktif öğrenci ve öğretmen ataması olmadığı garanti edilmiştir (handler validation).
/// </summary>
public static class BusinessSuspendedCoordinationConsumer
{
    public static void Consume(
        BusinessSuspended @event,
        IDocumentSession session)
    {
        CoordinationViewCleanup.DeleteAllRows(session, @event.BusinessId);
    }
}
