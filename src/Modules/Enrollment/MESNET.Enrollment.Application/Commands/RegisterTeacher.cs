namespace MESNET.Enrollment.Application.Commands;

/// <summary>
/// Kurum kimliği bu komutta <b>yoktur</b> (#309): handler onu kiracıdan türetir.
/// Gövdeden alındığında satır aktörün kiracısına düşüyor ama başka okulun kimliğini
/// taşıyabiliyordu — ölçüldü: 12 öğretmen satırı kendi kiracısıyla çelişiyordu.
/// </summary>
public sealed record RegisterTeacher(
    Guid KeycloakUserId,
    string FullName,
    string? BranchCode = null);
