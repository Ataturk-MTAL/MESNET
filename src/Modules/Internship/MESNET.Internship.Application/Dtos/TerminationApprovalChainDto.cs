namespace MESNET.Internship.Application.Dtos;

/// <summary>
/// Fesih onay zincirinin ham bayrakları (#218).
///
/// <para>Veli ve işletme yetkilisi bu zincirde <b>yoktur</b> — onlar fesih talep eder,
/// onaylamaz. Talebi kimin açtığı <c>RequestedBy</c>/<c>ReasonType</c> ile kaydedilir.</para>
/// </summary>
public sealed record TerminationApprovalChainDto(
    bool TeacherApproved,
    bool DeputyApproved,
    bool DirectorApproved,
    bool IsOverridden,
    string? OverriddenBy,
    DateTime? OverriddenAt,
    DateTime? CompletedAt);
