using Marten;
using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Application.Queries;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Handlers;

/// <summary>
/// Asgari ücret yürürlük geçmişini döndürür — en yeni dönem başta.
/// </summary>
/// <remarks>
/// Kurum kapsamı YOK (#147): parametre ulusaldır, her okul aynı zinciri görür. Bu yüzden
/// <c>ICurrentUserService</c> ile kapsam çözümü de gerekmez — ulusal parametreyi yazan
/// <c>SystemAdmin</c>'in kurumu olmadığı için kapsam aranması onu kendi yazdığı geçmişten
/// dışlardı.
/// </remarks>
public static class GetSalaryConfigHistoryHandler
{
    public static async Task<SalaryConfigHistoryDto> Handle(
        GetSalaryConfigHistory query, IQuerySession session)
    {
        var configs = await session.Query<SalaryCalculationConfig>()
            .OrderByDescending(c => c.EffectiveFrom)
            .ToListAsync();

        // Denetim adları kendi modülün UserNameView'ından çözülür; olayla ad taşınmaz (#137).
        var updaterIds = configs.Select(c => c.UpdatedById).Where(id => id != Guid.Empty).Distinct().ToArray();
        var names = updaterIds.Length == 0
            ? []
            : (await session.LoadManyAsync<UserNameView>(updaterIds))
                .ToDictionary(u => u.Id, u => u.FullName);

        var today = DateTime.UtcNow.Date;

        var items = configs
            .Select(c => new SalaryConfigDto(
                c.Id,
                c.MinimumWage,
                c.MinimumWageUnder16,
                c.EffectiveFrom,
                c.EffectiveTo,
                IsCurrent: c.EffectiveFrom.Date <= today
                           && (c.EffectiveTo is null || c.EffectiveTo.Value.Date >= today),
                IsScheduled: c.EffectiveFrom.Date > today,
                c.UpdatedById,
                names.GetValueOrDefault(c.UpdatedById),
                c.SmallBusinessRate,
                c.LargeBusinessRate,
                c.PersonnelThreshold,
                c.ApprenticeRate,
                c.MEM12thGradeRate,
                c.GovContribSmallNonMEM,
                c.GovContribLargeNonMEM,
                c.GovContribMEM))
            .ToList();

        return new SalaryConfigHistoryDto(items);
    }
}
