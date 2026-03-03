using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Extensions;

public static class EnrollmentMappingExtensions
{
    public static StudentProfileDto ToDto(this StudentProfile entity) => new(
        entity.Id,
        entity.KeycloakUserId,
        entity.FullName,
        entity.InstitutionId,
        entity.BranchCode,
        entity.BranchName,
        entity.SpecializationCode,
        entity.SpecializationName,
        entity.ClassYear,
        entity.Section,
        entity.StudentNumber,
        entity.PhoneNumber,
        entity.TcKimlikNo,
        entity.Guardian?.FullName,
        entity.Guardian?.Phone,
        entity.Status.Name,
        entity.Status.Slug,
        entity.RegisteredAt);

    public static TeacherProfileDto ToDto(this TeacherProfile entity) => new(
        entity.Id,
        entity.KeycloakUserId,
        entity.FullName,
        entity.InstitutionId,
        entity.RegisteredAt,
        entity.BranchCode);

    public static InternshipPlacementDto ToDto(
        this InternshipPlacement entity,
        string studentName = "",
        string businessName = "",
        string? teacherName = null) => new(
        entity.Id,
        entity.StudentId,
        studentName,
        entity.BusinessId,
        businessName,
        entity.InstitutionId,
        entity.TeacherId,
        teacherName,
        entity.Status.Name,
        entity.Status.Slug,
        entity.Source.Name,
        entity.Source.Slug,
        entity.PlacedAt,
        entity.TransferredAt,
        entity.TransferReason);
}
