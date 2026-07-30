using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Application.Queries;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Handlers;

/// <summary>
/// Kurumun asgari ücret yürürlük geçmişini döndürür — en yeni dönem başta.
/// </summary>
public static class GetSalaryConfigHistoryHandler
{
    public static async Task<SalaryConfigHistoryDto> Handle(
        GetSalaryConfigHistory query, IQuerySession session, ICurrentUserService currentUser)
    {
        // Kurum kapsamı token'dan okunur, istekten ALINMAZ (CLAUDE.md kesin kural).
        var institutionId = currentUser.GetCurrentUser()?.InstitutionId;
        if (institutionId is not { } scopedInstitutionId || scopedInstitutionId == Guid.Empty)
            throw new DomainException(PaymentErrors.InstitutionScopeMissing());

        var configs = await session.Query<SalaryCalculationConfig>()
            .Where(c => c.InstitutionId == scopedInstitutionId)
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
