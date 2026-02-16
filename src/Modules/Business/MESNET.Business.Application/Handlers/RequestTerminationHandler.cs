using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class RequestTerminationHandler
{
    public static async Task<BusinessTerminationRequested> Handle(RequestTermination command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        if (!business.Status.IsOperational)
            throw new InvalidOperationException(
                $"Sadece aktif işletmeler için fesih talebi oluşturulabilir. Mevcut durum: {business.Status.Slug}");

        return new BusinessTerminationRequested(business.Id, command.StudentId, command.Reason);
    }
}
