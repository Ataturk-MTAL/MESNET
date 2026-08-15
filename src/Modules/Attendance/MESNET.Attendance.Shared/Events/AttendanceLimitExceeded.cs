namespace MESNET.Attendance.Shared.Events;

public sealed record AttendanceLimitExceeded(
    Guid StudentId,
    Guid InstitutionId,
    Guid BusinessId,
    int TotalAbsenceDays,
    int Limit,
    /// <summary>
    /// Sayaç dönem bazlıdır (#242); görünüm bu olayı doğru satıra yönlendirebilmek için dönemi
    /// bilmek zorundadır.
    ///
    /// <para><b>Sona eklendi ve varsayılanı var:</b> saklı eski olaylar
    /// <c>Guid.Empty</c> deserialize olur ve mevcut tüketiciler (InternshipSaga) kırılmaz.</para>
    /// </summary>
    Guid AcademicPeriodId = default);
