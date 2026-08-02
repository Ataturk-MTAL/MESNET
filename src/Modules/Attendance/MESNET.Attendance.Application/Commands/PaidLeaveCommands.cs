using JasperFx;

namespace MESNET.Attendance.Application.Commands;

/// <summary>
/// Ücretli izin başvurusu aç (#177) — öğrenci.
///
/// <para><b><c>StudentId</c> istekten ALINMAZ:</b> uç onu token'ın <c>student_id</c> claim'inden
/// doldurur. İstekten alınsaydı bir öğrenci başkası adına izin başvurusu açabilirdi.</para>
/// </summary>
public sealed record RequestPaidLeave(
    DateTime StartDate,
    DateTime EndDate,
    string Reason)
{
    /// <summary>Token'daki <c>student_id</c> claim'i — uçta doldurulur.</summary>
    public Guid StudentId { get; init; }

    /// <summary>Seçili akademik dönem — uçta sorgu parametresinden doldurulur.</summary>
    public Guid AcademicPeriodId { get; init; }
}

/// <summary>
/// İşletme onayı (#177) — zincirin 1. adımı.
///
/// <para><b><c>BusinessIdClaim</c> istekten ALINMAZ:</b> uç onu token'ın <c>business_id</c>
/// claim'inden doldurur ve handler başvurunun işletmesiyle eşleşmesini şart koşar. İki taraflı
/// onayı ayakta tutan kontrol budur — <c>InstitutionManager</c> her wildcard'ı taşıdığı için
/// izin tek başına yetmez.</para>
/// </summary>
public sealed record BusinessApprovePaidLeave([property: Identity] Guid RequestId)
{
    public Guid BusinessIdClaim { get; init; }
}

/// <summary>Okul onayı (#177) — zincirin 2. adımı; izin bu komutla resmîleşir.</summary>
public sealed record ApprovePaidLeave([property: Identity] Guid RequestId);

/// <summary>
/// Ücretli izin başvurusunu reddet (#177). Her iki adımda da kullanılır; hangi adımda
/// reddedildiği aggregate'in o anki durumundan yazılır.
/// </summary>
public sealed record RejectPaidLeave(
    [property: Identity] Guid RequestId,
    string Reason)
{
    /// <summary>İşletme reddediyorsa token'daki <c>business_id</c>; okul reddediyorsa boş.</summary>
    public Guid BusinessIdClaim { get; init; }
}
