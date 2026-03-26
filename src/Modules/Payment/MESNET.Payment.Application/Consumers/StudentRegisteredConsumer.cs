using Marten;
using MESNET.Enrollment.Shared.Events;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Enrollment modülünden gelen StudentRegistered event'ini dinleyerek
/// Payment modülünün yerel öğrenci profilini oluşturur/günceller.
/// PaymentSummary'ye öğrenci adı/numarası/alan bilgisi denormalize etmek için gereklidir.
/// </summary>
public static class StudentRegisteredConsumer
{
    public static void Consume(StudentRegistered @event, IDocumentSession session)
    {
        var profile = new StudentPaymentProfile
        {
            Id = @event.StudentId,
            FullName = @event.FullName,
            StudentNumber = @event.StudentNumber,
            BranchCode = @event.BranchCode,
        };
        session.Store(profile);
    }
}
