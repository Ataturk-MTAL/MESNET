namespace MESNET.Internship.Core.ValueObjects;

public sealed record TerminationApprovalChain
{
    public bool ParentApproved { get; init; }
    public bool TeacherApproved { get; init; }
    public bool DeputyApproved { get; init; }
    public bool DirectorApproved { get; init; }
    public bool BusinessRepApproved { get; init; }
    public bool IsOverridden { get; init; }
    public string? OverriddenBy { get; init; }
    public DateTime? OverriddenAt { get; init; }
    public DateTime? CompletedAt { get; init; }

    public bool IsComplete(bool requiresParent) =>
        (!requiresParent || ParentApproved) &&
        TeacherApproved && DeputyApproved &&
        DirectorApproved && BusinessRepApproved;

    public bool IsCompleteOrOverridden(bool requiresParent) =>
        IsComplete(requiresParent) || IsOverridden;
}
