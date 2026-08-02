using MESNET.Common.Shared.Pagination;

namespace MESNET.Attendance.Application.Queries;

/// <summary>
/// Ücretli izin başvuru listesi (#177).
///
/// <para><b>Kapsam istekten alınmaz.</b> Hangi başvuruların görüneceğine handler karar verir:
/// okul tarafı kurumun tümünü, işletme yalnız kendi başvurularını, öğrenci yalnız kendisininkini
/// görür. Kapsam alanları token claim'lerinden doldurulur (bkz. <c>PaidLeaveEndpoints</c>).</para>
/// </summary>
public sealed record ListPaidLeaveRequests(string? Status = null) : PagedQuery
{
    /// <summary>Token'daki <c>business_id</c> claim'i — uçta doldurulur.</summary>
    public Guid? BusinessIdClaim { get; init; }

    /// <summary>Token'daki <c>student_id</c> claim'i — uçta doldurulur.</summary>
    public Guid? StudentIdClaim { get; init; }

    /// <summary>Token'daki <c>institution_id</c> claim'i — uçta doldurulur.</summary>
    public Guid? InstitutionIdClaim { get; init; }

    /// <summary>Seçili akademik dönem (isteğe bağlı filtre).</summary>
    public Guid? AcademicPeriodId { get; init; }
}
