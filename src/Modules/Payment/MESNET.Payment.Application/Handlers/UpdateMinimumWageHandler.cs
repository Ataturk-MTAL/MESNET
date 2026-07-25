using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Core.Entities;

namespace MESNET.Payment.Application.Handlers;

public static class UpdateMinimumWageHandler
{
    public static async Task Handle(UpdateMinimumWage command, IDocumentSession session)
    {
        // Yürürlükteki (henüz kapatılmamış) config. Birden fazla varsa en yenisi geçerlidir —
        // sıralama olmadan hangisinin geleceği belirsizdi (#75).
        var currentConfig = await session.Query<SalaryCalculationConfig>()
            .Where(c => c.InstitutionId == command.InstitutionId)
            .Where(c => c.EffectiveTo == null)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (currentConfig is not null)
        {
            // Aynı tarih için tekrar çağrı (ör. seeder'ın her çalıştırılışı): yeni satır açmak
            // yerine yerinde güncelle. Eskiden her çağrı bir kopya daha biriktiriyor ve eski
            // kaydı kendi başlangıcından bir gün öncesine kapatarak ters aralık üretiyordu (#75).
            if (command.EffectiveFrom == currentConfig.EffectiveFrom)
            {
                currentConfig.MinimumWage = command.NewMinimumWage;
                currentConfig.UpdatedBy = command.UpdatedBy;
                session.Store(currentConfig);
                return;
            }

            // Geriye dönük yürürlük eski kaydı kendi başlangıcından önceye kapatırdı.
            if (command.EffectiveFrom < currentConfig.EffectiveFrom)
                throw new DomainException(PaymentErrors.SalaryConfigBackdated(
                    command.EffectiveFrom, currentConfig.EffectiveFrom));

            currentConfig.EffectiveTo = command.EffectiveFrom.AddDays(-1);
            session.Store(currentConfig);
        }

        var newConfig = new SalaryCalculationConfig
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            MinimumWage = command.NewMinimumWage,
            EffectiveFrom = command.EffectiveFrom,
            UpdatedBy = command.UpdatedBy
        };
        session.Store(newConfig);
    }
}
