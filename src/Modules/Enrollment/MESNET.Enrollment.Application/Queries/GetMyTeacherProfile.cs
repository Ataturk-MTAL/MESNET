namespace MESNET.Enrollment.Application.Queries;

/// <summary>
/// Oturumdaki kullanıcının öğretmen kaydı. Kimlik istekten değil, endpoint'te token'dan
/// (<c>sub</c>) okunur — başka bir kullanıcının kaydını sorgulamak için kullanılamaz.
/// </summary>
public sealed record GetMyTeacherProfile(Guid KeycloakUserId);
