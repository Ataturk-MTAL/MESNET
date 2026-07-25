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
            // Olay ikisini de zaten taşıyordu ama atılıyordu; MESEM 12. sınıf taban ücret oranı
            // (%50) ve MESEM devlet katkısı (tamamı) bu alanlar olmadan seçilemiyor (#64).
            ClassYear = @event.ClassYear,
            EducationTypeName = @event.EducationType,
        };
        session.Store(profile);
    }
}
