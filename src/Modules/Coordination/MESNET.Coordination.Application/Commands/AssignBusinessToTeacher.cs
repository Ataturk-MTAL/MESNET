namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Koordinatörlük ataması alan bazlıdır — hedef satır
/// <c>(BusinessId, BranchCode, AcademicPeriodId)</c> üçlüsüyle belirlenir (#114).
///
/// <para>Atamayı yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar.
/// <c>TeacherName</c> ayrı bir şeydir: atamanın <b>hedefi</b>, işlemi yapan aktör değil.</para>
/// </summary>
public sealed record AssignBusinessToTeacher(
    Guid BusinessId,
    Guid TeacherId,
    string TeacherName,
    int AssignedHours,
    string AssignedDay,
    int? PeriodNumber,
    Guid InstitutionId,
    string BranchCode = "",
    Guid AcademicPeriodId = default);
