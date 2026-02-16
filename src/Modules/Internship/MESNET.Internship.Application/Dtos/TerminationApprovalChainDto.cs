namespace MESNET.Internship.Application.Dtos;

public sealed record TerminationApprovalChainDto(
    bool ParentApproved,
    bool TeacherApproved,
    bool DeputyApproved,
    bool DirectorApproved,
    bool BusinessRepApproved,
    bool IsOverridden,
    string? OverriddenBy,
    DateTime? OverriddenAt,
    DateTime? CompletedAt);
