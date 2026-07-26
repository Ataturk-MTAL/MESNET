using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Core.ReadModels;

namespace MESNET.Enrollment.Application.Consumers;

/// <summary>
/// İşletmenin alan yetkilerini Enrollment'ın kendi şemasına denormalize eder (#119).
/// Yerleştirme guard'ı (<c>PlaceStudentHandler</c>) bu read-model'e bakar.
/// </summary>
public static class BusinessBranchesAuthorizedConsumer
{
    public static void Consume(BusinessBranchesAuthorized @event, IDocumentSession session)
    {
        var view = new BusinessBranchAuthorizationView
        {
            Id = @event.BusinessId,
            BusinessName = @event.BusinessName,
            ActiveBranchCodes = [.. @event.ActiveBranchCodes],
            LastUpdated = DateTime.UtcNow
        };

        session.Store(view);
    }
}
