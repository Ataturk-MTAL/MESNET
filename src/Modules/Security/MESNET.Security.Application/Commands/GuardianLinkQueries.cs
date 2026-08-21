namespace MESNET.Security.Application.Commands;

/// <summary>Velisi bağlı olmayan öğrenciler (#271).</summary>
public sealed record GetStudentsWithoutGuardian;

/// <param name="TotalStudents">Kiracıdaki toplam öğrenci sayısı.</param>
/// <param name="MissingCount">Velisi bağlı olmayan öğrenci sayısı — asıl ölçüt.</param>
public sealed record GuardianLinkGapResult(
    int TotalStudents,
    int MissingCount,
    IReadOnlyList<StudentWithoutGuardianDto> Students);

public sealed record StudentWithoutGuardianDto(
    Guid StudentId, string FullName, string? StudentNumber, string? BranchCode);
