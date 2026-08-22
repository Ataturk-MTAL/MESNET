using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Enums;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Policies;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Core.ValueObjects;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class RegisterStudentHandler
{
    public static async Task<(StudentProfileDto, StudentRegistered)> Handle(RegisterStudent command, IDocumentSession session)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(EnrollmentErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(EnrollmentErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        if (!EducationType.TryFromName(command.EducationType, ignoreCase: true, out var educationType))
            throw new DomainException(EnrollmentErrors.InvalidEducationType(command.EducationType));

        // Geçersiz kategori sessizce "Öğrenci"ye düşerse aday çırak/çırak yanlış ücret tabanı
        // alır; açıkça hata ver (#85).
        if (!StudentCategory.TryFromName(command.Category, ignoreCase: true, out var studentCategory))
            throw new DomainException(EnrollmentErrors.InvalidStudentCategory(command.Category));

        // Doğal anahtar: (AcademicPeriodId, StudentNumber) — #237. Anahtar HAM değer üzerine
        // kurulsaydı " 1101" ile "1101" iki ayrı satır olur ve kısıt hiçbir şey korumazdı.
        // Numarasız öğrenci kısıtın dışındadır: numara bugün zorunlu değil ve zorunlu kılmak
        // mevcut kayıtları kırardı.
        var studentNumber = StudentNumberPolicy.Normalize(command.StudentNumber);
        if (StudentNumberPolicy.RequiresUniqueness(studentNumber)
            && await session.Query<StudentProfile>().AnyAsync(s =>
                s.AcademicPeriodId == command.AcademicPeriodId && s.StudentNumber == studentNumber))
        {
            throw new DomainException(EnrollmentErrors.StudentNumberAlreadyRegistered(studentNumber!));
        }

        var student = new StudentProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode,
            BranchName = command.BranchName,
            SpecializationCode = command.SpecializationCode,
            SpecializationName = command.SpecializationName,
            ClassYear = command.ClassYear,
            EducationType = educationType,
            EducationTypeName = educationType.Name,
            Section = command.Section,
            // Normalleştirilmiş değer saklanır — kısıt da, kontrol de bunun üzerine kurulu.
            StudentNumber = studentNumber,
            PhoneNumber = command.PhoneNumber,
            TcKimlikNo = command.TcKimlikNo,
            HasJourneymanQualification = command.HasJourneymanQualification,
            BirthDate = command.BirthDate,
            Category = studentCategory,
            CategoryName = studentCategory.Name
        };

        if (command.GuardianName is not null || command.GuardianPhone is not null)
        {
            student.Guardian = new GuardianInfo(
                command.GuardianName ?? "",
                "",
                command.GuardianPhone ?? "",
                "",
                "");
        }

        session.Store(student);

        return (student.ToDto(), new StudentRegistered(student.Id, student.FullName, student.InstitutionId, student.AcademicPeriodId, student.BranchCode, student.ClassYear, educationType.Name, student.StudentNumber ?? "", student.HasJourneymanQualification, student.BirthDate, student.Category.Name, student.KeycloakUserId));
    }
}
