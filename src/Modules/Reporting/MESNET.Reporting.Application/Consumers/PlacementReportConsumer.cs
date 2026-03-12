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
    public static async Task Consume(StudentRegistered @event, IDocumentSession session)
    {
        // Henüz placement yapılmamış — öğrenci bilgilerini sakla
        // StudentPlaced event'i geldiğinde BusinessId ile eşleştirilecek
        var view = new StudentPlacementReportView
        {
            Id = @event.StudentId, // PlacementId henüz yok, StudentId kullan
            StudentId = @event.StudentId,
            StudentName = @event.FullName,
            StudentNumber = "", // StudentRegistered'da okul no yok
            ClassName = $"{@event.BranchCode} - {(@event.ClassYear > 0 ? @event.ClassYear.ToString() : "?")}",
            ClassYear = @event.ClassYear,
            BranchCode = @event.BranchCode,
            BranchName = "", // Enrollment event'inde BranchName yok
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId
        };

        session.Store(view);
    }

    public static async Task Consume(StudentPlaced @event, IDocumentSession session)
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
