using Marten;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Shared.Events;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Core.Models;
using MESNET.Reporting.Core.ReadModels;
using Wolverine;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Haftalık ziyaret ataması yapılınca Form 3 (Günlük Rehberlik Formu) PDF'lerini otomatik üretir.
/// Her atama (öğretmen-işletme-gün) için bir adet karekodlu PDF oluşturulur ve MinIO'ya arşivlenir.
/// </summary>
public static class WeeklyVisitsGeneratedConsumer
{
    public static async Task Consume(
        WeeklyVisitsGenerated @event,
        IQuerySession querySession,
        IMessageBus bus,
        CancellationToken ct)
    {
        var systemUser = new UserContext(Guid.Empty, "Sistem (Otomatik Rapor)");

        foreach (var assignment in @event.Assignments)
        {
            // Bu işletmedeki öğrenci sayısını al
            var studentCount = await querySession.Query<StudentPlacementReportView>()
                .Where(s => s.BusinessId == assignment.BusinessId
                         && s.InstitutionId == @event.InstitutionId
                         && s.AcademicPeriodId == @event.AcademicPeriodId)
                .CountAsync(ct);

            var formData = new GuidanceVisitFormData
            {
                DocumentId = assignment.AssignmentId, // QR kod kaynağı
                BusinessId = assignment.BusinessId,
                InstitutionId = @event.InstitutionId,
                TeacherId = assignment.TeacherId,
                TeacherName = assignment.TeacherName,
                BusinessName = assignment.BusinessName,
                BranchName = assignment.BranchName,
                StudentCount = studentCount,
                VisitDate = assignment.VisitDate.ToDateTime(TimeOnly.MinValue),
                // İmza alanları — işletme yetkili adı event'ten gelmez, öğretmen yazdırıp elle doldurur
                BusinessContactName = null,
                VicePrincipalName = null,
                // Serbest metin alanları boş — öğretmen yazdırdıktan sonra elle doldurur
                NegativeFactors = null,
                GuidanceActions = null,
                ReportNotes = null,
            };

            var command = new GenerateGuidanceVisitDocument(formData, systemUser);
            await bus.InvokeAsync(command, ct);
        }
    }
}
