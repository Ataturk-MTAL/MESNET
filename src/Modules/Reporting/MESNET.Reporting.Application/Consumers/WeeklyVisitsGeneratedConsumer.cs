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
            // Bu işletmedeki öğrenci listesini al
            var students = await querySession.Query<StudentPlacementReportView>()
                .Where(s => s.BusinessId == assignment.BusinessId
                         && s.InstitutionId == @event.InstitutionId
                         && s.AcademicPeriodId == @event.AcademicPeriodId)
                .ToListAsync(ct);

            var formData = new GuidanceVisitFormData
            {
                DocumentId = assignment.AssignmentId, // QR kod kaynağı
                BusinessId = assignment.BusinessId,
                InstitutionId = @event.InstitutionId,
                TeacherId = assignment.TeacherId,
                InstitutionName = "", // Reporting modülünde kurum adı read model'i yok — PDF'te boş kalır
                TeacherName = assignment.TeacherName,
                BusinessName = assignment.BusinessName,
                BusinessAddress = assignment.BusinessAddress ?? "",
                VisitDate = assignment.VisitDate.ToDateTime(TimeOnly.MinValue),
                StudentNotes = students.Select(s =>
                    new StudentVisitEntry(s.StudentName, "")).ToList()
                // Gözlem alanları boş — öğretmen yazdırdıktan sonra elle doldurur
            };

            var command = new GenerateGuidanceVisitDocument(formData, systemUser);
            await bus.InvokeAsync(command, ct);
        }
    }
}
