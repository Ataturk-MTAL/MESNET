using Marten;
using MESNET.Enrollment.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Enrollment modülü event'lerini dinleyerek StudentPlacementReportView read model'ini günceller.
/// Öğrenci-işletme-alan eşleşmesi bilgisi sağlar.
/// </summary>
public static class PlacementReportConsumer
{
    /// <summary>Öğrenci kaydı olayı — canlı yol.</summary>
    public static Task Consume(StudentRegistered @event, IDocumentSession session)
        => ApplyStudent(@event.ToSnapshot(), session);

    /// <summary>
    /// Onarım yolu (#290): <c>POST /api/students/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentRegistered</c> yeniden yayınlanamaz — tüketicilerinden biri şube sayacını
    /// ARTIRIYOR ve her yeniden yayın sayacı şişirirdi.
    /// </summary>
    public static Task Consume(StudentSnapshotResynced @event, IDocumentSession session)
        => ApplyStudent(@event, session);

    private static async Task ApplyStudent(StudentSnapshotResynced @event, IDocumentSession session)
    {
        // Henüz placement yapılmamış — öğrenci bilgilerini sakla
        // StudentPlaced event'i geldiğinde BusinessId ile eşleştirilecek
        var view = new StudentPlacementReportView
        {
            Id = @event.StudentId, // PlacementId henüz yok, StudentId kullan
            StudentId = @event.StudentId,
            StudentName = @event.FullName,
            StudentNumber = @event.StudentNumber,
            ClassName = $"{@event.BranchCode} - {(@event.ClassYear > 0 ? @event.ClassYear.ToString() : "?")}",
            ClassYear = @event.ClassYear,
            BranchCode = @event.BranchCode,
            BranchName = "", // Enrollment event'inde BranchName yok
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId
        };

        session.Store(view);
    }

    /// <summary>
    /// Yerleştirme yaşam döngüsü olayı — canlı yol.
    /// </summary>
    public static Task Consume(StudentPlaced @event, IDocumentSession session)
        => Apply(@event.ToSnapshot(), session);

    /// <summary>
    /// Onarım yolu (#291): <c>POST /api/placements/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentPlaced</c> yeniden yayınlanamaz — o, saga'nın başlatıcı olayıdır ve yeniden
    /// yayını tekil kısıt ihlaliyle ölü mektuba düşerdi (uç yine 200 dönerek).
    /// </summary>
    public static Task Consume(PlacementSnapshotResynced @event, IDocumentSession session)
        => Apply(@event, session);

    private static async Task Apply(PlacementSnapshotResynced @event, IDocumentSession session)
    {
        // Mevcut view'ı bul (StudentRegistered ile oluşturulmuş olabilir)
        var view = await session.Query<StudentPlacementReportView>()
            .Where(v => v.StudentId == @event.StudentId && v.AcademicPeriodId == @event.AcademicPeriodId)
            .FirstOrDefaultAsync();

        if (view is null)
        {
            // StudentRegistered henüz gelmemişse yeni oluştur
            view = new StudentPlacementReportView
            {
                Id = @event.PlacementId,
                StudentId = @event.StudentId,
                InstitutionId = @event.InstitutionId,
                AcademicPeriodId = @event.AcademicPeriodId
            };
        }

        view.BusinessId = @event.BusinessId;
        view.BusinessName = @event.BusinessName;
        view.BranchCode = @event.BranchCode;
        view.BranchName = @event.BranchName;
        view.TeacherId = @event.TeacherId;

        session.Store(view);
    }
}
