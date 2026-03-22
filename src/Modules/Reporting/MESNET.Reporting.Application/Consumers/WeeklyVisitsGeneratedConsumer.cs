using Marten;
using MESNET.Coordination.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Haftalık ziyaret ataması yapılınca VisitAssignmentReportView read model'ini günceller.
/// Bu view Form 3 (Günlük Rehberlik Formu) toplu üretiminin veri kaynağıdır.
///
/// NOT: PDF üretimi burada yapılmaz — öğretmen bazlı aylık gruplama için
/// "Belge Oluştur → Form 3" akışı kullanılır. Her haftalık plan event'inde
/// ayrı PDF üretmek yanlış olur; öğretmenin tüm ay ziyaretleri tek PDF'te toplanmalıdır.
/// </summary>
public static class WeeklyVisitsGeneratedConsumer
{
    public static async Task Consume(
        WeeklyVisitsGenerated @event,
        IDocumentSession session,
        CancellationToken ct)
    {
        foreach (var assignment in @event.Assignments)
        {
            var studentCount = await session.Query<StudentPlacementReportView>()
                .Where(s => s.BusinessId == assignment.BusinessId
                         && s.InstitutionId == @event.InstitutionId
                         && s.AcademicPeriodId == @event.AcademicPeriodId)
                .CountAsync(ct);

            session.Store(new VisitAssignmentReportView
            {
                Id = assignment.AssignmentId,
                TeacherId = assignment.TeacherId,
                TeacherName = assignment.TeacherName,
                BusinessId = assignment.BusinessId,
                BusinessName = assignment.BusinessName,
                BranchCode = assignment.BranchCode,
                BranchName = assignment.BranchName,
                VisitDate = assignment.VisitDate,
                StudentCount = studentCount,
                InstitutionId = @event.InstitutionId,
                AcademicPeriodId = @event.AcademicPeriodId,
            });
        }
    }
}
