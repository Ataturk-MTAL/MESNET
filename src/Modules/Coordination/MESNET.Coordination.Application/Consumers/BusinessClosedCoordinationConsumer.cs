using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme kapatıldığında coordination view satırlarını kalıcı olarak siler.
/// İşletmenin <b>tüm alan satırları</b> + temel satırı silinir.
/// Bu noktada aktif öğrenci ve öğretmen ataması olmadığı garanti edilmiştir (handler validation).
/// </summary>
public static class BusinessClosedCoordinationConsumer
{
    public static void Consume(
        BusinessClosed @event,
        IDocumentSession session)
    {
        CoordinationViewCleanup.DeleteAllRows(session, @event.BusinessId);
    }
}
