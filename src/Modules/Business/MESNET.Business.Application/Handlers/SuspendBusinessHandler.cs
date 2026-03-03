using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class SuspendBusinessHandler
{
    public static async Task<BusinessSuspended> Handle(
        SuspendBusiness command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // 1. İşletme kontrolü
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, cancellationToken);
        if (business is null)
        {
            throw new DomainException(new Error("BUSINESS_NOT_FOUND", $"İşletme bulunamadı: {command.BusinessId}"));
        }

        // 2. Durumu kontrol et
        if (business.Status == BusinessStatus.Closed)
        {
            throw new DomainException(new Error("BUSINESS_ALREADY_CLOSED", "İşletme zaten kapalı."));
        }

        // 3. Aktif öğrenci / öğretmen kontrolü
        if (business.Capacity.OccupiedSlots > 0)
            throw new DomainException(BusinessErrors.HasActiveStudents(command.BusinessId));

        if (business.HasAssignedTeacher)
            throw new DomainException(BusinessErrors.HasAssignedTeacher(command.BusinessId));

        // 4. İşletmeyi pasife al (Inactive status)
        business.Status = BusinessStatus.Inactive;

        session.Store(business);
        await session.SaveChangesAsync(cancellationToken);

        // 4. Event döndür
        return new BusinessSuspended(
            command.BusinessId,
            command.SuspendedBy,
            command.Reason,
            DateTime.UtcNow
        );
    }
}
