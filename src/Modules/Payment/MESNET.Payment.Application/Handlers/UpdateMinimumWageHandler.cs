using Marten;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Core.Entities;

namespace MESNET.Payment.Application.Handlers;

public static class UpdateMinimumWageHandler
{
    public static async Task Handle(UpdateMinimumWage command, IDocumentSession session)
    {
        // Önceki config'i expire et
        var currentConfig = session.Query<SalaryCalculationConfig>()
            .Where(c => c.InstitutionId == command.InstitutionId)
            .Where(c => c.EffectiveTo == null)
            .FirstOrDefault();

        if (currentConfig is not null)
        {
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
