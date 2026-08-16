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
    Guid AcademicPeriodId = default,
    /// <summary>
    /// Sınırın hangi ayaktan dolduğu: <c>Unexcused</c> (özürsüz) ya da <c>Total</c> (toplam) —
    /// md. 36 (5) örgünde ikisini de bağlayıcı kılar (#183). Fesih gerekçesinde hangi ayağın
    /// dolduğu yazılabilmeli: "10 gün özürsüz" ile "30 gün toplam" idare için aynı şey değildir.
    ///
    /// <para><b>Sona eklendi ve varsayılanı boş:</b> saklı eski olaylar bozulmadan
    /// deserialize olur; tüketiciler kırılmaz.</para>
    /// </summary>
    string LimitKind = "");
