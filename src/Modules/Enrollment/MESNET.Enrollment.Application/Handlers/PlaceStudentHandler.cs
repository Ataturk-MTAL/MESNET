using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.Policies;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class PlaceStudentHandler
{
    public static async Task<(InternshipPlacementDto, StudentPlaced)> Handle(PlaceStudent command, IDocumentSession session)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(EnrollmentErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(EnrollmentErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        var student = await session.LoadAsync<StudentProfile>(command.StudentId)
            ?? throw new DomainException(EnrollmentErrors.StudentNotFound(command.StudentId));

        if (!student.Status.CanTransitionTo(StudentStatus.Placed))
            throw new DomainException(
                EnrollmentErrors.InvalidTransition("Öğrenci", student.Status.Slug, StudentStatus.Placed.Slug));

        // İşletme yoksa okulda stajdır (#159): staj yeri bulunamayan öğrenci stajını okulda
        // yapar. Ücret ve devlet katkısı doğmaz — ikisini de 3308 AYRI AYRI kapsam dışı tutuyor
        // (Madde 25 ve Geçici Madde 12, aynı cümle). Sözleşme kurulmadığı için maaş dönemi de
        // açılmaz: dönemler sözleşmeden doğar (#154).
        var placementType = command.BusinessId.HasValue ? PlacementType.Business : PlacementType.School;

        // İşletme doğrulamalarının tamamı yalnız işletmeli yerleştirmede geçerlidir.
        BusinessProfileView? business = null;
        if (command.BusinessId is { } businessId)
        {
            business = await session.LoadAsync<BusinessProfileView>(businessId)
                ?? throw new DomainException(EnrollmentErrors.BusinessNotFound(businessId));

            if (!business.IsActive)
                throw new DomainException(EnrollmentErrors.BusinessNotActive);

            if (business.AvailableCapacity <= 0)
                throw new DomainException(EnrollmentErrors.BusinessCapacityFull);

            // İşletme yalnız idarece yetkilendirildiği alanlardan öğrenci alabilir (#119).
            // Yetki iptali geçmiş yerleştirmeleri bozmaz — yalnız YENİ yerleştirme reddedilir.
            var branchAuthorization = await session.LoadAsync<BusinessBranchAuthorizationView>(businessId);
            if (!PlacementBranchPolicy.IsBusinessAuthorizedFor(branchAuthorization, student.BranchCode))
                throw new DomainException(EnrollmentErrors.BusinessNotAuthorizedForBranch(
                    business.BusinessName, student.BranchName));
        }

        var placement = new InternshipPlacement
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            TeacherId = command.TeacherId,
            StudentName = student.FullName,
            BranchCode = student.BranchCode,
            Source = ApplicationSource.InstitutionAssignment,
            Type = placementType,
            TypeName = placementType.Name
        };

        student.Status = StudentStatus.Placed;

        session.Store(placement);
        session.Store(student);

        // TeacherId varsa isim yükle
        string? teacherName = null;
        if (command.TeacherId.HasValue)
        {
            var teacher = await session.LoadAsync<TeacherProfile>(command.TeacherId.Value);
            teacherName = teacher?.FullName;
        }

        // Okulda stajda gösterilecek bir işletme adı yok; UI türü kendi metniyle yazar.
        return (placement.ToDto(business?.BusinessName ?? "", teacherName), new StudentPlaced(
            placement.Id,
            placement.StudentId,
            placement.BusinessId,
            placement.InstitutionId,
            placement.AcademicPeriodId,
            placement.TeacherId,
            placement.PlacedAt,
            StudentName: student.FullName,
            BusinessName: business?.BusinessName ?? "",
            BranchCode: student.BranchCode,
            BranchName: student.BranchName,
            PlacementType: placementType.Name));
    }
}
