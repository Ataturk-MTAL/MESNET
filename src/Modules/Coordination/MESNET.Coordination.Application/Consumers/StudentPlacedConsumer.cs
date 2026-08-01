using Marten;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Öğrenci bir işletmeye yerleştirildiğinde o işletmenin <b>ilgili alan satırını</b> günceller.
///
/// <para>Kimlik <c>(BusinessId, BranchCode, AcademicPeriodId)</c> üçlüsüdür: aynı işletmeye
/// ikinci bir alandan öğrenci gelirse ayrı bir satır açılır — birinci alanın satırı ezilmez
/// (#114).</para>
///
/// <para>Öğrenci sayısı <c>++</c> ile artırılmaz, Coordination'ın kendi yerleştirme
/// read-model'inden (<see cref="CoordinationPlacedStudentView"/>) <b>yeniden hesaplanır</b>.
/// Böylece olay yeniden oynatıldığında sayaç şişmez (idempotent).</para>
/// </summary>
public static class StudentPlacedConsumer
{
    public static async Task Consume(
        StudentPlaced @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // Okulda staj (#159): işletme yok, ziyaret edilecek yer yok, koordinasyon saati
        // doğmaz. Satır kimliği (BusinessId, BranchCode, AcademicPeriodId) üçlüsünden
        // üretildiği için zaten kurulamaz — burada açıkça çıkılıyor ki niyet görünsün.
        if (@event.BusinessId is not { } businessId) return;

        var rowId = CoordinationViewId.For(
            businessId, @event.BranchCode, @event.AcademicPeriodId);

        var row = await session.LoadAsync<BusinessCoordinationView>(rowId, cancellationToken);

        row ??= await CreateBranchRowAsync(rowId, businessId, @event, session, cancellationToken);

        row.Name = @event.BusinessName.Length > 0 ? @event.BusinessName : row.Name;
        row.BranchCode = @event.BranchCode;
        row.BranchName = @event.BranchName;
        row.ActiveStudentCount = await CountActiveStudentsAsync(@event, session, cancellationToken);

        session.Store(row);
    }

    /// <summary>
    /// Alan satırını işletme düzeyi temel satırdan türeterek oluşturur.
    /// Temel satır yoksa (olay sıralama garantisi yok) olaydaki bilgilerle asgari satır kurulur.
    /// </summary>
    private static async Task<BusinessCoordinationView> CreateBranchRowAsync(
        Guid rowId,
        Guid businessId,
        StudentPlaced @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var baseRow = await session.LoadAsync<BusinessCoordinationView>(
            CoordinationViewId.Base(businessId), cancellationToken);

        var row = new BusinessCoordinationView
        {
            Id = rowId,
            BusinessId = businessId,
            Name = baseRow?.Name ?? @event.BusinessName,
            Address = baseRow?.Address,
            District = baseRow?.District,
            Location = baseRow?.Location,
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId,
            BranchCode = @event.BranchCode,
            BranchName = @event.BranchName,
        };

        if (baseRow is not null)
            DistanceHelper.CopyDistanceTo(baseRow, row);

        return row;
    }

    /// <summary>
    /// (İşletme, alan, dönem) üçlüsündeki aktif yerleştirme sayısı.
    /// Bu olayın kendi <c>PlacementId</c>'si sonuca dahil edilir: <see cref="PlacedStudentViewConsumer"/>
    /// ayrı bir handler olduğu için henüz yazmamış olabilir; küme kullanıldığından
    /// yeniden oynatmada da mükerrer sayım olmaz.
    /// </summary>
    private static async Task<int> CountActiveStudentsAsync(
        StudentPlaced @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var placements = await session.Query<CoordinationPlacedStudentView>()
            .Where(p =>
                p.BusinessId == @event.BusinessId &&
                p.BranchCode == @event.BranchCode &&
                p.AcademicPeriodId == @event.AcademicPeriodId &&
                p.IsActive)
            .ToListAsync(cancellationToken);

        var placementIds = placements.Select(p => p.Id).ToHashSet();
        placementIds.Add(@event.PlacementId);

        return placementIds.Count;
    }
}
