using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class PlaceStudentHandler
{
    public static async Task<StudentPlaced> Handle(PlaceStudent command, IDocumentSession session)
    {
        var student = await session.LoadAsync<StudentProfile>(command.StudentId)
            ?? throw new InvalidOperationException($"Öğrenci bulunamadı: {command.StudentId}");

        if (!student.Status.CanTransitionTo(StudentStatus.Placed))
            throw new InvalidOperationException(
                $"Öğrenci '{student.Status.Slug}' durumundan '{StudentStatus.Placed.Slug}' durumuna geçirilemez.");

        var business = await session.LoadAsync<BusinessProfileView>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

        if (!business.IsActive)
            throw new InvalidOperationException("İşletme aktif değil, yerleştirme yapılamaz.");

        if (business.AvailableCapacity <= 0)
            throw new InvalidOperationException("İşletme kapasitesi dolu, yerleştirme yapılamaz.");

        var placement = new InternshipPlacement
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            TeacherId = command.TeacherId,
            Source = ApplicationSource.InstitutionAssignment
        };

        student.Status = StudentStatus.Placed;

        session.Store(placement);
        session.Store(student);

        return new StudentPlaced(
            placement.Id,
            placement.StudentId,
            placement.BusinessId,
            placement.InstitutionId,
            placement.PlacedAt);
    }
}
