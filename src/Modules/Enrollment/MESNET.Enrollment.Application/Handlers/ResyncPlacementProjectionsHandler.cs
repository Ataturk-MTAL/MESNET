using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;
using Wolverine;

namespace MESNET.Enrollment.Application.Handlers;

public static class ResyncPlacementProjectionsHandler
{
    public static async Task<ResyncPlacementProjectionsResult> Handle(
        ResyncPlacementProjections command, IQuerySession session, IMessageBus bus, CancellationToken ct)
    {
        // SmartEnum LINQ'te karşılaştırılamaz; düz string kopyası StatusName kullanılıyor
        // (bkz. CLAUDE.md — Marten SmartEnum LINQ kuralları).
        var finalStatuses = PlacementStatus.List
            .Where(s => s.IsFinal)
            .Select(s => s.Name)
            .ToArray();

        // Tamamlanmış/fesihli yerleştirmeler yeniden yayınlanmaz — tüketici modüllerde
        // yeniden "aktif" işaretlenmeleri yanlış olurdu.
        var placements = await session.Query<InternshipPlacement>()
            .Where(p => !finalStatuses.Contains(p.StatusName))
            .ToListAsync(ct);

        var published = 0;
        var skipped = 0;

        foreach (var placement in placements)
        {
            // StudentPlaced'i 4 consumer tüketiyor ve StudentName/BusinessName/BranchName
            // alanlarını denormalize tutuyor. Bu adlar olmadan yayınlarsak onların verisini
            // boş string'le eziyoruz — kaynak kayıt eksikse yayınlamak yerine atlıyoruz.
            var student = await session.LoadAsync<StudentProfile>(placement.StudentId, ct);

            // Okulda staj yerleştirmesinde işletme YOKTUR ve bu eksik veri değildir (#159) —
            // atlanmamalı, yoksa o öğrencinin projeksiyonları hiç dolmaz.
            var business = placement.BusinessId is { } bid
                ? await session.LoadAsync<BusinessProfileView>(bid, ct)
                : null;

            if (student is null || (placement.BusinessId.HasValue && business is null))
            { skipped++; continue; }

            await bus.PublishAsync(new StudentPlaced(
                placement.Id,
                placement.StudentId,
                placement.BusinessId,
                placement.InstitutionId,
                placement.AcademicPeriodId,
                placement.TeacherId,
                placement.PlacedAt,
                StudentName: student.FullName,
                BusinessName: business?.BusinessName ?? "",
                BranchCode: student.BranchCode,
                BranchName: student.BranchName,
                PlacementType: placement.Type.Name));

            published++;
        }

        return new ResyncPlacementProjectionsResult(published, skipped);
    }
}
