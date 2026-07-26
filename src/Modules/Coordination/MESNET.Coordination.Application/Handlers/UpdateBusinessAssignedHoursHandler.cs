using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class UpdateBusinessAssignedHoursHandler
{
    public static async Task Handle(
        UpdateBusinessAssignedHours command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await CoordinationViewLookup.LoadBranchRowAsync(
            session, command.BusinessId, command.BranchCode, command.AcademicPeriodId, cancellationToken);

        if (view is null)
        {
            throw new DomainException(
                CoordinationErrors.BusinessBranchNotFound(command.BusinessId, command.BranchCode));
        }

        // Fahri ziyaret ücret doğurmaz → saat her zaman 0'a sabitlenir (#115). Kullanıcı
        // fahri işaretlerken girdide eski saat kalmış olabilir; komutu reddetmek yerine
        // sıfırlıyoruz, istenen sonuç zaten "0 saat".
        var newHours = command.IsHonoraryVisit ? 0 : command.AssignedHours;

        if (!command.IsHonoraryVisit)
        {
            if (command.AssignedHours <= 0)
                throw new DomainException(CoordinationErrors.InvalidAssignedHours(command.AssignedHours));

            if (command.AssignedHours > view.MaxCoordinationHours)
            {
                throw new DomainException(
                    CoordinationErrors.AssignedHoursExceedMax(command.AssignedHours, view.MaxCoordinationHours));
            }

            // Havuz ve öğretmen kapasitesi kısıtları yalnız ücretli satır için anlamlı —
            // fahri satır her iki toplama da 0 katkı yapar, kontrol edilirse yalnızca
            // başkalarının aşımı yüzünden haksız yere reddedilir.
            await ValidateWorkloadPoolAsync(session, view, newHours, cancellationToken);
            await ValidateTeacherLimitAsync(session, view, newHours, cancellationToken);
        }

        ApplyChange(view, command, newHours);
        session.Store(view);
    }

    /// <summary>
    /// Havuz kısıtı: alandaki toplam takdir edilen saat ≤ TotalWorkloadPool.
    /// Fahri satırlar <see cref="BusinessCoordinationView.AssignedHours"/> = 0 ile saklandığı
    /// için toplama kendiliğinden girmez (#115). Bayrağa göre SQL filtresi konmaz: alan eski
    /// kayıtların JSON'unda yok, <c>NOT NULL</c> üç değerli mantıkta NULL döner ve o satırlar
    /// toplamdan sessizce düşerdi.
    /// </summary>
    private static async Task ValidateWorkloadPoolAsync(
        IDocumentSession session,
        BusinessCoordinationView view,
        int newHours,
        CancellationToken cancellationToken)
    {
        var workloadConfig = await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == view.InstitutionId &&
                c.BranchCode == view.BranchCode &&
                c.AcademicPeriodId == view.AcademicPeriodId,
                cancellationToken);

        if (workloadConfig is null) return;

        var otherAssigned = await session.Query<BusinessCoordinationView>()
            .Where(b =>
                b.InstitutionId == view.InstitutionId &&
                b.BranchCode == view.BranchCode &&
                b.AcademicPeriodId == view.AcademicPeriodId &&
                b.Id != view.Id)
            .SumAsync(b => b.AssignedHours, cancellationToken);

        var totalAssigned = otherAssigned + newHours;

        if (totalAssigned > workloadConfig.TotalWorkloadPool)
        {
            throw new DomainException(
                CoordinationErrors.WorkloadPoolExceeded(totalAssigned, workloadConfig.TotalWorkloadPool));
        }
    }

    /// <summary>
    /// Öğretmen başına azami ek ders saati kontrolü (öğretmen atanmışsa).
    /// Diğer işletmelerin katkısı <see cref="BusinessCoordinationView.BillableTargetHours"/>
    /// ile hesaplanır: fahri satır 0, takdir edilmemiş satır mesafe tavanı (#115).
    /// </summary>
    private static async Task ValidateTeacherLimitAsync(
        IDocumentSession session,
        BusinessCoordinationView view,
        int newHours,
        CancellationToken cancellationToken)
    {
        if (!view.AssignedTeacherId.HasValue) return;

        var config = await session.Query<CoordinationConfig>()
            .FirstOrDefaultAsync(c => c.InstitutionId == view.InstitutionId, cancellationToken);

        if (config is null) return;

        // Select projection yerine tam belge: yeni `isHonoraryVisit` alanı eski kayıtların
        // JSON'unda yok, anonim tip projeksiyonunda NULL → bool dönüşümü patlardı.
        var otherBusinesses = await session.Query<BusinessCoordinationView>()
            .Where(b =>
                b.AssignedTeacherId == view.AssignedTeacherId &&
                b.Id != view.Id)
            .ToListAsync(cancellationToken);

        var otherTeacherHours = otherBusinesses.Sum(b => b.BillableTargetHours());
        var teacherTotal = otherTeacherHours + newHours;

        if (teacherTotal > config.MaxWeeklyExtraHours)
        {
            throw new DomainException(
                CoordinationErrors.TeacherHoursExceedMax(
                    view.AssignedTeacherId.Value, teacherTotal, config.MaxWeeklyExtraHours));
        }
    }

    private static void ApplyChange(
        BusinessCoordinationView view,
        UpdateBusinessAssignedHours command,
        int newHours)
    {
        var oldHours = view.AssignedHours;
        var wasHonorary = view.IsHonoraryVisit;

        view.AssignedHours = newHours;
        view.IsHonoraryVisit = command.IsHonoraryVisit;

        view.History.Insert(0, new AssignmentHistoryEntry(
            DateTime.UtcNow,
            "HoursUpdated",
            command.UpdatedBy,
            view.AssignedTeacherName,
            null,
            null,
            newHours,
            DescribeChange(oldHours, wasHonorary, newHours, command.IsHonoraryVisit)));

        view.LastModifiedAt = DateTime.UtcNow;
        view.LastModifiedBy = command.UpdatedBy;
    }

    /// <summary>Geçmiş kaydı açıklaması — fahri geçişleri saat değişiminden ayrı okunur olsun.</summary>
    private static string DescribeChange(int oldHours, bool wasHonorary, int newHours, bool isHonorary)
    {
        if (isHonorary && !wasHonorary)
            return $"Fahri (ücretsiz) ziyaret olarak işaretlendi; takdir edilen saat {oldHours} → 0";

        if (!isHonorary && wasHonorary)
            return $"Fahri ziyaret işareti kaldırıldı; takdir edilen saat 0 → {newHours}";

        if (isHonorary)
            return "Fahri (ücretsiz) ziyaret olarak korundu; takdir edilen saat 0";

        return $"Takdir edilen saat {oldHours} → {newHours} olarak değiştirildi";
    }
}
