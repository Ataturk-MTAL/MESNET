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
                currentConfig.MinimumWageUnder16 = command.NewMinimumWageUnder16;
                currentConfig.UpdatedBy = command.UpdatedBy;
                RefreshStatutoryRates(currentConfig);
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
            MinimumWageUnder16 = command.NewMinimumWageUnder16,
            EffectiveFrom = command.EffectiveFrom,
            UpdatedBy = command.UpdatedBy
        };
        session.Store(newConfig);
    }

    /// <summary>
    /// Kanunla belirlenmiş oranları koddaki güncel değerlere çeker.
    /// </summary>
    /// <remarks>
    /// Bu oranlar kuruma özel tercih değil, 3308 Madde 25 ve Geçici Madde 12'de yazılı sabitler.
    /// Kayıtlı document'ta eski değer kalırsa (ör. kırpılmış <c>0.3333</c> yerine tam <c>1/3</c>)
    /// koddaki düzeltme mevcut veride etkisiz kalıyordu (#83). Mevzuat değişirse koddaki
    /// varsayılan güncellenir ve asgari ücret güncellemesiyle birlikte yayılır.
    ///
    /// İşletmenin daha yüksek ücret ödemesi bu oranlarla değil, sözleşmedeki ücretle temsil
    /// edilmelidir (Madde 25: ücret sözleşmeyle tespit edilir).
    /// </remarks>
    private static void RefreshStatutoryRates(SalaryCalculationConfig config)
    {
        var defaults = new SalaryCalculationConfig();

        config.PersonnelThreshold = defaults.PersonnelThreshold;
        config.LargeBusinessRate = defaults.LargeBusinessRate;
        config.SmallBusinessRate = defaults.SmallBusinessRate;
        config.MEM12thGradeRate = defaults.MEM12thGradeRate;
        config.ApprenticeRate = defaults.ApprenticeRate;
        config.GovContribSmallNonMEM = defaults.GovContribSmallNonMEM;
        config.GovContribLargeNonMEM = defaults.GovContribLargeNonMEM;
        config.GovContribMEM = defaults.GovContribMEM;
    }
}
