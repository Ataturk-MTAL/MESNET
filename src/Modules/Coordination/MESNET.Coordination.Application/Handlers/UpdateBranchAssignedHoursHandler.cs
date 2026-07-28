using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Security;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Coordination.Core.Services;

namespace MESNET.Coordination.Application.Handlers;

/// <summary>
/// Bir alanın saat dağıtımını tek transaction'da doğrulayıp yazar (#117).
///
/// <para>Handler yalnız <b>yükleme ve kaydetme</b> yapar; kısıt mantığının tamamı saf
/// <see cref="BranchHoursPolicy"/> içindedir (Marten oturumu gerektirmeden birim testlenebilsin).</para>
///
/// <para>Wolverine <c>AutoApplyTransactions</c> ile oturum handler sonunda <b>bir kez</b>
/// commit edilir: doğrulama fırlatırsa hiçbir <c>Store</c> çağrısı veritabanına inmez.</para>
/// </summary>
public static class UpdateBranchAssignedHoursHandler
{
    public static async Task Handle(
        UpdateBranchAssignedHours command,
        IDocumentSession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        ValidateShape(command);

        // Kapsam kontrolü kısıt doğrulamasından ÖNCE: yetkisiz istek hiçbir satır okumadan reddedilir (#126).
        BranchScopeGuard.EnsureCanWrite(currentUser, command.BranchCode);

        var branchRows = await LoadBranchRowsAsync(session, command, cancellationToken);
        var targets = await ResolveTargetsAsync(session, command, branchRows, cancellationToken);

        var changedIds = targets.Select(t => t.Row.Id).ToHashSet();

        var input = new BranchHoursValidationInput(
            Changes: [.. targets.Select(ToChange)],
            OtherBillableHours: branchRows
                .Where(r => !changedIds.Contains(r.Id))
                .Sum(r => r.BillableHours()),
            TotalWorkloadPool: await LoadWorkloadPoolAsync(session, command, cancellationToken),
            OtherTeacherBillableHours: await LoadOtherTeacherHoursAsync(
                session, targets, changedIds, cancellationToken),
            MaxWeeklyExtraHours: await LoadMaxWeeklyExtraHoursAsync(session, command, cancellationToken));

        var violation = BranchHoursPolicy.Validate(input);
        if (violation is not null)
        {
            throw new DomainException(CoordinationErrors.BranchHoursConstraintViolated(violation));
        }

        // Aktör token'dan gelir, istekten DEĞİL (#137).
        var updatedById = currentUser.GetUserId();

        foreach (var (row, item) in targets)
        {
            AssignedHoursMutation.Apply(
                row,
                newHours: item.IsHonoraryVisit ? 0 : item.AssignedHours,
                isHonoraryVisit: item.IsHonoraryVisit,
                updatedById: updatedById);

            session.Store(row);
        }
    }

    /// <summary>İsteğin biçimsel geçerliliği — kısıt mantığından önce gelir.</summary>
    private static void ValidateShape(UpdateBranchAssignedHours command)
    {
        if (string.IsNullOrWhiteSpace(command.BranchCode) || command.AcademicPeriodId == Guid.Empty)
            throw new DomainException(CoordinationErrors.BranchScopeRequired());

        if (command.Items is null || command.Items.Count == 0)
            throw new DomainException(CoordinationErrors.EmptyBranchHoursBatch());

        var duplicate = command.Items
            .GroupBy(i => i.BusinessId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
            throw new DomainException(CoordinationErrors.DuplicateBranchHoursItem(duplicate.Key));
    }

    /// <summary>
    /// Alanın tüm satırları. Havuz toplamı <b>değişmeyen</b> satırları da içerdiği için
    /// set tek sorguda çekilir — satır başına ayrı sorgu atmak issue #117'nin kökündeki
    /// sıraya bağlılığı geri getirirdi.
    /// </summary>
    private static async Task<IReadOnlyList<BusinessCoordinationView>> LoadBranchRowsAsync(
        IDocumentSession session,
        UpdateBranchAssignedHours command,
        CancellationToken cancellationToken) =>
        await session.Query<BusinessCoordinationView>()
            .Where(b =>
                b.InstitutionId == command.InstitutionId &&
                b.BranchCode == command.BranchCode &&
                b.AcademicPeriodId == command.AcademicPeriodId)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Her istek satırını gerçek koordinasyon satırıyla eşler. Alan sorgusu tutmazsa
    /// (çok-alanlı modele geçmeden önce yazılmış kayıt) <see cref="CoordinationViewLookup"/>
    /// yedeğine düşer; o da bulamazsa istek tümden reddedilir.
    /// </summary>
    private static async Task<List<(BusinessCoordinationView Row, BranchAssignedHoursItem Item)>>
        ResolveTargetsAsync(
            IDocumentSession session,
            UpdateBranchAssignedHours command,
            IReadOnlyList<BusinessCoordinationView> branchRows,
            CancellationToken cancellationToken)
    {
        var targets = new List<(BusinessCoordinationView, BranchAssignedHoursItem)>();

        foreach (var item in command.Items)
        {
            var row = branchRows.FirstOrDefault(r => r.ResolveBusinessId() == item.BusinessId)
                      ?? await CoordinationViewLookup.LoadBranchRowAsync(
                          session, item.BusinessId, command.BranchCode, command.AcademicPeriodId, cancellationToken);

            if (row is null)
            {
                throw new DomainException(
                    CoordinationErrors.BusinessBranchNotFound(item.BusinessId, command.BranchCode));
            }

            targets.Add((row, item));
        }

        return targets;
    }

    private static BranchHoursChange ToChange(
        (BusinessCoordinationView Row, BranchAssignedHoursItem Item) target) =>
        new(
            BusinessId: target.Row.ResolveBusinessId(),
            BusinessName: target.Row.Name,
            RequestedHours: target.Item.AssignedHours,
            IsHonoraryVisit: target.Item.IsHonoraryVisit,
            MaxCoordinationHours: target.Row.MaxCoordinationHours,
            AssignedTeacherId: target.Row.AssignedTeacherId,
            AssignedTeacherName: target.Row.AssignedTeacherName);

    /// <summary>Havuz yapılandırması yoksa <c>null</c> — kısıt uygulanmaz.</summary>
    private static async Task<int?> LoadWorkloadPoolAsync(
        IDocumentSession session,
        UpdateBranchAssignedHours command,
        CancellationToken cancellationToken)
    {
        var config = await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == command.InstitutionId &&
                c.BranchCode == command.BranchCode &&
                c.AcademicPeriodId == command.AcademicPeriodId,
                cancellationToken);

        return config?.TotalWorkloadPool;
    }

    private static async Task<int?> LoadMaxWeeklyExtraHoursAsync(
        IDocumentSession session,
        UpdateBranchAssignedHours command,
        CancellationToken cancellationToken)
    {
        var config = await session.Query<CoordinationConfig>()
            .FirstOrDefaultAsync(c => c.InstitutionId == command.InstitutionId, cancellationToken);

        return config?.MaxWeeklyExtraHours;
    }

    /// <summary>
    /// Etkilenen öğretmenlerin <b>değişmeyen</b> satırlarından gelen hedef saat toplamı.
    /// Öğretmen yükü alanla sınırlı değildir (bir öğretmen birden çok alana bakabilir),
    /// bu yüzden alan filtresi olmadan öğretmen bazında sorgulanır.
    ///
    /// <para>Select projeksiyonu bilinçli olarak kullanılmaz: <c>isHonoraryVisit</c> anahtarı
    /// #115 öncesi JSON'da yok, anonim tip projeksiyonu NULL → bool dönüşümüne çarpardı.</para>
    /// </summary>
    private static async Task<Dictionary<Guid, int>> LoadOtherTeacherHoursAsync(
        IDocumentSession session,
        List<(BusinessCoordinationView Row, BranchAssignedHoursItem Item)> targets,
        HashSet<Guid> changedIds,
        CancellationToken cancellationToken)
    {
        var teacherIds = targets
            .Select(t => t.Row.AssignedTeacherId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var totals = new Dictionary<Guid, int>();

        foreach (var teacherId in teacherIds)
        {
            var rows = await session.Query<BusinessCoordinationView>()
                .Where(b => b.AssignedTeacherId == teacherId)
                .ToListAsync(cancellationToken);

            totals[teacherId] = rows
                .Where(r => !changedIds.Contains(r.Id))
                .Sum(r => r.BillableTargetHours());
        }

        return totals;
    }
}
