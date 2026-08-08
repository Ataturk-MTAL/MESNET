namespace MESNET.Internship.Shared.Events;

/// <summary>
/// Fesih onay zinciri kapandı — müdür onayı ya da override (#220).
///
/// <para>Bu olay <b>fesih kararının kesinleştiği andır</b>. Enrollment onu tüketip öğrenciyi
/// okula alır: eski yerleştirme kapatılır, yerine işverensiz (okulda staj) yerleştirme açılır.
/// Kural: <i>"öğrenci fesih yaptığı anda otomatikmen okula atanır"</i>.</para>
///
/// <para><b>Modüller arası olduğu için olayla taşınır</b>, doğrudan çağrıyla değil —
/// Internship, Enrollment'ın komutunu çağıramaz.</para>
/// </summary>
/// <param name="BusinessId">Ayrılınan işletme — okulda stajda <c>null</c> (#159).</param>
public sealed record InternshipTerminationCompleted(
    Guid InternshipId,
    Guid StudentId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? BusinessId);
