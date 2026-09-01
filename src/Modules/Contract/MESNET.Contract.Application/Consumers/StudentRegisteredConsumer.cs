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
    /// <summary>Öğrenci kaydı olayı — canlı yol.</summary>
    public static void Consume(StudentRegistered @event, IDocumentSession session)
        => Apply(@event.ToSnapshot(), session);

    /// <summary>
    /// Onarım yolu (#290): <c>POST /api/students/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentRegistered</c> yeniden yayınlanamaz — tüketicilerinden biri şube sayacını
    /// ARTIRIYOR ve her yeniden yayın sayacı şişirirdi.
    /// </summary>
    public static void Consume(StudentSnapshotResynced @event, IDocumentSession session)
        => Apply(@event, session);

    private static void Apply(StudentSnapshotResynced @event, IDocumentSession session)
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
