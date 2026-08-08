using Marten;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;
using MESNET.Internship.Shared.Events;
using Microsoft.Extensions.Logging;

namespace MESNET.Enrollment.Application.Consumers;

/// <summary>
/// Fesih kesinleşince öğrenciyi <b>okula alır</b> (#220).
///
/// <para>Kural: <i>"öğrenci fesih yaptığı anda otomatikmen okula, yani alan şefine atanır."</i>
/// Öğrenci dönem bitmeden yeni işletme bulamazsa alan şefi not ve devamsızlık sürecini takip
/// ederek dönemi tamamlatır.</para>
///
/// <para><b>Alan şefi bir kimlik DEĞİLDİR</b>, dolayısıyla yerleştirmeye yazılmaz. Alan şefliği
/// <c>DepartmentHead</c> rolü + <c>branch_codes</c> kapsamıdır (#126); alan şefi bu yerleştirmeyi
/// <b>kendi branş kapsamından</b> görür. <c>TeacherId</c> boş bırakılır.</para>
///
/// <para><b>Ek ücret doğmaz</b> — koordinasyon ücreti <c>BusinessCoordinationView</c> üzerinden
/// işletme başına hesaplanır ve işverensiz yerleştirme hiç işletme üretmez. Bunun için ek kod
/// gerekmiyor; kural yapısal olarak sağlanıyor.</para>
///
/// <para><b>Neden bu iş burada:</b> eski yerleştirmeyi kapatan hiçbir mekanizma yoktu —
/// <c>PlacementStatus.Cancelled</c> tanımlıydı ama hiçbir yerde kullanılmıyordu. Staj
/// feshedilse bile yerleştirme aktif kalıyordu.</para>
/// </summary>
public static class InternshipTerminationCompletedConsumer
{
    public static async Task Consume(
        InternshipTerminationCompleted @event,
        IDocumentSession session,
        ILogger<InternshipTerminationCompleted> logger)
    {
        var student = await session.LoadAsync<StudentProfile>(@event.StudentId);
        if (student is null)
        {
            logger.LogWarning(
                "Fesih sonrası okula atama atlandı — öğrenci kaydı yok: {StudentId}", @event.StudentId);
            return;
        }

        var active = await ActivePlacementAsync(session, @event.StudentId, @event.AcademicPeriodId);

        // Idempotent: Wolverine yeniden deneyebilir. Açık bir okul yerleştirmesi zaten varsa
        // ikincisi açılmaz — yoksa her yeniden denemede bir kopya doğardı.
        if (active is { Type.Name: nameof(PlacementType.School) })
        {
            logger.LogInformation(
                "Öğrenci zaten okulda staja alınmış, atlandı: {StudentId}", @event.StudentId);
            return;
        }

        if (active is not null)
        {
            active.Status = PlacementStatus.Cancelled;
            session.Store(active);
        }

        var placement = new InternshipPlacement
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            // İşverensiz: okulda staj (#159).
            BusinessId = null,
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId,
            // Alan şefi kimlik olarak yazılmaz; kapsamdan görülür (#126).
            TeacherId = null,
            StudentName = student.FullName,
            BranchCode = student.BranchCode,
            Source = ApplicationSource.InstitutionAssignment,
            Type = PlacementType.School
        };

        student.Status = StudentStatus.Placed;

        session.Store(placement);
        session.Store(student);

        // Diğer modüllerin görünümleri bu olayla beslenir — okul yerleştirmesi de normal bir
        // yerleştirmedir, ayrı bir kanal açılmaz.
        await session.SaveChangesAsync();

        logger.LogInformation(
            "Fesih sonrası okula alındı: öğrenci {StudentId}, yerleştirme {PlacementId}",
            student.Id, placement.Id);
    }

    /// <summary>
    /// Dönem içindeki açık yerleştirme. Kapanmış (iptal/tamamlanmış) kayıtlar aranmaz —
    /// yoksa eski bir fesih kaydı "aktif" sanılır.
    /// </summary>
    private static Task<InternshipPlacement?> ActivePlacementAsync(
        IDocumentSession session, Guid studentId, Guid academicPeriodId)
    {
        var openStatuses = new[]
        {
            PlacementStatus.Matched.Name,
            PlacementStatus.Active.Name
        };

        return session.Query<InternshipPlacement>()
            .Where(p => p.StudentId == studentId
                     && p.AcademicPeriodId == academicPeriodId
                     && openStatuses.Contains(p.StatusName))
            .OrderByDescending(p => p.PlacedAt)
            .FirstOrDefaultAsync();
    }
}
