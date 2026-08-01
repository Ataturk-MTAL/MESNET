using Marten;
using MESNET.Payment.Application.Queries;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;

namespace MESNET.Payment.Application.Handlers;

/// <summary>
/// Sınıf tekrarı nedeniyle devlet katkısı bloke olan öğrencileri döndürür (#161).
/// </summary>
public static class GetContributionBlockedStudentsHandler
{
    public static async Task<ContributionBlockedStudentsResult> Handle(
        GetContributionBlockedStudents query, IQuerySession session)
    {
        var activePeriodIds = await session.Query<AcademicPeriodView>()
            .Where(p => p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        if (activePeriodIds.Count == 0)
            return new ContributionBlockedStudentsResult([]);

        // Yürürlükteki dönemde açılmış kayıtlar bloke ÜRETMEZ: onlar bu yılın normal ayları.
        // Bloke yalnız önceki bir dönemde açılmış kayıtla, öğrenci hâlâ aynı sınıftaysa doğar.
        var pastClaims = await session.Query<ClassYearContributionClaim>()
            .Where(c => !activePeriodIds.Contains(c.FirstAcademicPeriodId))
            .ToListAsync();

        if (pastClaims.Count == 0)
            return new ContributionBlockedStudentsResult([]);

        var profiles = await session.LoadManyAsync<StudentPaymentProfile>(
            pastClaims.Select(c => c.StudentId).Distinct().ToArray());

        var currentClassYears = profiles.ToDictionary(p => p.Id, p => p.ClassYear);

        // Öğrenci terfi ettiyse eski kaydın sınıfı artık tutmaz — bloke düşer, katkı yeniden işler.
        var blocked = pastClaims
            .Where(c => currentClassYears.TryGetValue(c.StudentId, out var year) && year == c.ClassYear)
            .Select(c => new ContributionBlockedStudentDto(c.StudentId, c.ClassYear, c.FirstClaimedMonth))
            .ToList();

        return new ContributionBlockedStudentsResult(blocked);
    }
}
