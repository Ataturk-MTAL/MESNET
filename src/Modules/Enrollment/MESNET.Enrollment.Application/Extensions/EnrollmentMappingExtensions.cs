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
        entity.ClassYear,
        entity.Section,
        entity.Status.Name,
        entity.Status.Slug,
        entity.RegisteredAt);

    public static TeacherProfileDto ToDto(this TeacherProfile entity) => new(
        entity.Id,
        entity.KeycloakUserId,
        entity.FullName,
        entity.InstitutionId,
        entity.RegisteredAt);

    public static InternshipPlacementDto ToDto(this InternshipPlacement entity) => new(
        entity.Id,
        entity.StudentId,
        entity.BusinessId,
        entity.InstitutionId,
        entity.TeacherId,
        entity.Status.Name,
        entity.Status.Slug,
        entity.Source.Name,
        entity.Source.Slug,
        entity.PlacedAt,
        entity.TransferredAt,
        entity.TransferReason);
}
