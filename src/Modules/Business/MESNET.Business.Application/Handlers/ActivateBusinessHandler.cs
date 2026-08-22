using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class ActivateBusinessHandler
{
    public static async Task<BusinessActivated> Handle(ActivateBusiness command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        if (!business.Status.CanTransitionTo(BusinessStatus.Active))
            throw new DomainException(BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Active.Slug));

        // ⚠ BİLİNÇLİ ASİMETRİ (#151): kapatma yeter sayı ister, AÇMA istemez. Herhangi bir
        // okuldan tek yetkili açabilir — yani iki okulun kararını tek okul geri alabilir.
        // Hata değildir: yanlış kapatılmış işletme, yanlış açık kalandan daha zararlıdır
        // (süzgeçten düşer, öğrenci yerleştirilemez, sözleşme yapılamaz). Sistem AÇIK kalmaya
        // doğru hata yapmalıdır. BU YORUM SİLİNİRSE biri bunu hata olarak açar.
        //
        // Bildirimler temizlenir; kalsalardı yeter sayı hâlâ dolu olur ve bir sonraki
        // bildirimde işletme anında yeniden kapanırdı.
        business.ClosureReports.Clear();
        business.ClosedAt = null;
        business.Status = BusinessStatus.Active;

        session.Store(business);

        return new BusinessActivated(business.Id, business.RegisteredByInstitutionId, business.Name, business.Address, business.Location);
    }
}
