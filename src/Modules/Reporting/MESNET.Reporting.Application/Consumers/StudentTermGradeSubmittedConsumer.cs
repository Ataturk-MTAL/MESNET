using Marten;
using MESNET.Coordination.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// İşletme dönem notlarını gönderdiğinde (Coordination.StudentTermGradeSubmitted) Reporting'in
/// StudentTermGradeView read-model'ini oluşturur/günceller — Dönem Not Fişi bundan üretilir.
/// </summary>
public static class StudentTermGradeSubmittedConsumer
{
    public static void Consume(StudentTermGradeSubmitted @event, IDocumentSession session)
    {
        session.Store(new StudentTermGradeView
        {
            Id = @event.StudentTermGradeId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId,
            PracticeGrades = @event.PracticeGrades,
            ServiceGrades = @event.ServiceGrades,
            ProjectGrades = @event.ProjectGrades,
            ExperimentGrades = @event.ExperimentGrades,
            TermAverage = @event.TermAverage,
            MasterInstructorName = @event.MasterInstructorName,
            SubmittedAt = @event.SubmittedAt,
        });
    }
}
