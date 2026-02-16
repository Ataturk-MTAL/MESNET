using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateMinimumWageHandler
{
    public static async Task<MinimumWageUpdated> Handle(UpdateMinimumWage command, IDocumentSession session)
    {
        var param = await session.Query<SystemParameter>()
            .FirstOrDefaultAsync(p => p.Key == "MinimumWage");

        if (param is null)
        {
            param = new SystemParameter
            {
                Id = Guid.NewGuid(),
                Key = "MinimumWage",
                Value = command.Amount.ToString("F2"),
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            param.Value = command.Amount.ToString("F2");
            param.UpdatedAt = DateTime.UtcNow;
        }

        session.Store(param);

        return new MinimumWageUpdated(command.InstitutionId, command.Amount, command.EffectiveDate);
    }
}
