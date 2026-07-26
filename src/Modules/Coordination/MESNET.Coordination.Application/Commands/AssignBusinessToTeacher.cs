namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Koordinatörlük ataması alan bazlıdır — hedef satır
/// <c>(BusinessId, BranchCode, AcademicPeriodId)</c> üçlüsüyle belirlenir (#114).
/// </summary>
public sealed record AssignBusinessToTeacher(
    Guid BusinessId,
    Guid TeacherId,
    string TeacherName,
    int AssignedHours,
    string AssignedDay,
    int? PeriodNumber,
    Guid InstitutionId,
    string AssignedBy,
    string BranchCode = "",
    Guid AcademicPeriodId = default);
