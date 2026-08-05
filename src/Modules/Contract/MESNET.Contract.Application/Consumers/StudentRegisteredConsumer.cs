using Marten;
using MESNET.Contract.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Contract.Application.Consumers;

/// <summary>
/// Enrollment'tan gelen StudentRegistered event'ini dinleyip lokal StudentNameView'ı günceller.
/// Sözleşme listelemesinde öğrenci ad/numara araması bu view üzerinden yapılır.
/// </summary>
public static class StudentRegisteredConsumer
{
    public static void Consume(StudentRegistered @event, IDocumentSession session)
    {
        session.Store(new StudentNameView
        {
            Id = @event.StudentId,
            InstitutionId = @event.InstitutionId,
            FullName = @event.FullName,
            StudentNumber = string.IsNullOrWhiteSpace(@event.StudentNumber) ? null : @event.StudentNumber
        });
    }
}
