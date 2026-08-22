using Marten;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

/// <summary>
/// Eski tek-satır koordinasyon kayıtlarını çok-alanlı modele taşır (#114).
///
/// Kaynaklar tamamen Coordination şemasındadır — başka modülün şemasına sorgu atılmaz:
/// işletme bilgileri mevcut satırlardan, alan/dönem kırılımı ve öğrenci sayısı
/// Coordination'ın kendi <see cref="CoordinationPlacedStudentView"/> read-model'inden gelir.
///
/// İşlem idempotenttir: ikinci kez çalıştırıldığında aynı kimlikler üretilir, satır sayısı
/// ve öğrenci sayaçları değişmez.
/// </summary>
public static class ResyncCoordinationViewsHandler
{
    public static async Task<ResyncCoordinationViewsResult> Handle(
        ResyncCoordinationViews command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var existingRows = await session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == command.InstitutionId)
            .ToListAsync(cancellationToken);

        if (existingRows.Count == 0)
            return new ResyncCoordinationViewsResult(0, 0, 0);

        var placements = await session.Query<CoordinationPlacedStudentView>()
            .Where(p => p.InstitutionId == command.InstitutionId && p.IsActive)
            .ToListAsync(cancellationToken);

        var rowsByBusiness = existingRows
            .GroupBy(v => v.ResolveBusinessId())
            .ToDictionary(g => g.Key, g => g.ToList());

        var rebuilt = new Dictionary<Guid, BusinessCoordinationView>();

        // ── 1) İşletme düzeyi temel satırlar ──
        foreach (var (businessId, rows) in rowsByBusiness)
        {
            var master = PickMaster(businessId, rows);
            var baseId = CoordinationViewId.Base(businessId);

            rebuilt[baseId] = CopyBusinessFacts(master, new BusinessCoordinationView
            {
                Id = baseId,
                BusinessId = businessId,
                InstitutionId = command.InstitutionId,
            });
        }

        var baseRowCount = rebuilt.Count;

        // ── 2) Alan satırları — kaynak: Coordination'ın kendi yerleştirme read-model'i ──
        var placementGroups = placements
            .Where(p => !string.IsNullOrWhiteSpace(p.BranchCode))
            .GroupBy(p => (p.BusinessId, p.BranchCode, p.AcademicPeriodId));

        foreach (var group in placementGroups)
        {
            var (businessId, branchCode, academicPeriodId) = group.Key;

            if (!rowsByBusiness.TryGetValue(businessId, out var rows))
                continue; // işletmenin coordination kaydı yok (kapatılmış olabilir)

            var master = PickMaster(businessId, rows);
            var rowId = CoordinationViewId.For(businessId, branchCode, academicPeriodId);

            var row = CopyBusinessFacts(master, new BusinessCoordinationView
            {
                Id = rowId,
                BusinessId = businessId,
                InstitutionId = command.InstitutionId,
                AcademicPeriodId = academicPeriodId,
                BranchCode = branchCode,
                BranchName = group.First().BranchName,
                // Sayaç yeniden hesaplanır — eski satırın şişmiş değeri taşınmaz
                ActiveStudentCount = group.Select(p => p.Id).Distinct().Count(),
            });

            // Aynı alan+döneme ait eski satırın atama verisi korunur
            var previous = rows.FirstOrDefault(r =>
                string.Equals(r.BranchCode, branchCode, StringComparison.OrdinalIgnoreCase) &&
                r.AcademicPeriodId == academicPeriodId);

            if (previous is not null) CopyAssignment(previous, row);

            rebuilt[rowId] = row;
        }

        // ── 3) Yeni şemaya girmeyen eski satırları sil ──
        var removed = 0;
        foreach (var old in existingRows)
        {
            if (rebuilt.ContainsKey(old.Id)) continue;
            session.Delete(old);
            removed++;
        }

        foreach (var row in rebuilt.Values)
            session.Store(row);

        return new ResyncCoordinationViewsResult(
            baseRowCount, rebuilt.Count - baseRowCount, removed);
    }

    /// <summary>
    /// İşletme bilgilerinin kaynağı: önce yeni şemadaki temel satır, sonra eski tek-satır
    /// kaydı, o da yoksa mesafesi bilinen ilk satır.
    /// </summary>
    private static BusinessCoordinationView PickMaster(
        Guid businessId, List<BusinessCoordinationView> rows)
    {
        var baseId = CoordinationViewId.Base(businessId);

        return rows.FirstOrDefault(r => r.Id == baseId)
               ?? rows.FirstOrDefault(r => r.Id == businessId)
               ?? rows.OrderByDescending(r => r.DistanceToSchoolKm.HasValue).First();
    }

    private static BusinessCoordinationView CopyBusinessFacts(
        BusinessCoordinationView master, BusinessCoordinationView target)
    {
        target.Name = master.Name;
        target.Address = master.Address;
        target.District = master.District;
        target.Location = master.Location;
        target.DistanceToSchoolKm = master.DistanceToSchoolKm;
        target.IsManualDistance = master.IsManualDistance;
        target.MaxCoordinationHours = master.MaxCoordinationHours;
        return target;
    }

    private static void CopyAssignment(
        BusinessCoordinationView previous, BusinessCoordinationView target)
    {
        target.AssignedHours = previous.AssignedHours;
        target.IsHonoraryVisit = previous.IsHonoraryVisit;
        target.AssignedTeacherId = previous.AssignedTeacherId;
        target.AssignedTeacherName = previous.AssignedTeacherName;
        target.AssignedDay = previous.AssignedDay;
        target.AssignedPeriodNumber = previous.AssignedPeriodNumber;
        target.AssignedSlots = [.. previous.AssignedSlots];
        target.History = [.. previous.History];
        target.LastModifiedAt = previous.LastModifiedAt;
        target.LastModifiedById = previous.LastModifiedById;
    }
}
