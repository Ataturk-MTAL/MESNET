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
/// Bir öğretmenin tüm ziyaretleri tek PDF'te toplanır (ikişerli A5 yerleşim — kağıt tasarrufu).
/// </summary>
public static class WeeklyVisitsGeneratedConsumer
{
    public static async Task Consume(
        WeeklyVisitsGenerated @event,
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken ct)
    {
        var systemUser = new UserContext(Guid.Empty, "Sistem (Otomatik Rapor)");

        // Öğretmen bazlı grupla — her öğretmen için tek PDF
        var byTeacher = @event.Assignments.GroupBy(a => a.TeacherId);

        foreach (var teacherGroup in byTeacher)
        {
            var forms = new List<GuidanceVisitFormData>();

            foreach (var assignment in teacherGroup)
            {
                // Bu işletmedeki öğrencileri al
                var students = await session.Query<StudentPlacementReportView>()
                    .Where(s => s.BusinessId == assignment.BusinessId
                             && s.InstitutionId == @event.InstitutionId
                             && s.AcademicPeriodId == @event.AcademicPeriodId)
                    .ToListAsync(ct);

                // TeacherName'i placement view'lara yaz (batch generate için gerekli)
                foreach (var s in students.Where(s => s.TeacherId == assignment.TeacherId
                                                      && string.IsNullOrEmpty(s.TeacherName)))
                {
                    s.TeacherName = assignment.TeacherName;
                    session.Store(s);
                }

                forms.Add(new GuidanceVisitFormData
                {
                    DocumentId = assignment.AssignmentId,
                    BusinessId = assignment.BusinessId,
                    InstitutionId = @event.InstitutionId,
                    TeacherId = assignment.TeacherId,
                    TeacherName = assignment.TeacherName,
                    BusinessName = assignment.BusinessName,
                    BranchName = assignment.BranchName,
                    StudentCount = students.Count,
                    VisitDate = assignment.VisitDate.ToDateTime(TimeOnly.MinValue),
                });
            }

            var first = teacherGroup.First();
            var pageCount = (forms.Count + 1) / 2;

            var command = new GenerateGuidanceVisitBatchDocument(
                forms, systemUser,
                InstitutionId: @event.InstitutionId,
                TeacherId: first.TeacherId,
                Description: $"{first.TeacherName} — {forms.Count} ziyaret ({pageCount} sayfa)");

            await bus.InvokeAsync(command, ct);
        }
    }
}
