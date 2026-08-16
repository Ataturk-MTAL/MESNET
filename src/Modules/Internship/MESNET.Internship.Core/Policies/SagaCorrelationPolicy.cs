using MESNET.Internship.Core.Enums;

namespace MESNET.Internship.Core.Policies;

/// <summary>
/// Başka modülün olayı hangi staj saga'sına gider (#248).
///
/// <para><b>Neden gerekti:</b> Wolverine saga kimliğini mesajın alan ADINDAN çözer —
/// <c>InternshipSaga</c> için <c>InternshipId</c>. Internship'in kendi mesajları bu adı taşıyor
/// ve çalışıyor; <b>başka modülün olayları taşımıyor</b> ve
/// <c>IndeterminateSagaStateIdException</c> ile ölü mektup kuyruğuna düşüyordu. Ölçüldü: 2248
/// saga'nın <b>hiçbirinde</b> <c>contractId</c> yazılı değildi, 2139'u sözleşme aktifken bile
/// <c>AwaitingContract</c>'ta çakılıydı ve devamsızlık sınırı her tetiklendiğinde mesaj ölü
/// mektuba düşüyordu.</para>
///
/// <para><b>Neden olaya alan eklenmiyor:</b> saga kimliği Internship'in iç bilgisidir
/// (<c>Guid.NewGuid()</c> ile doğar). Onu <c>Contract.Shared</c> ya da
/// <c>Attendance.Shared</c>'e taşımak, o modüllere Internship'in kimlik kavramını sızdırırdı.
/// <c>PaymentSaga</c>'da aynı sorun <c>[SagaIdentityFrom]</c> ile çözülmüştü çünkü orada
/// olaylar zaten saga anahtarını (<c>SalaryPeriodId</c>) taşıyordu — burada taşımıyorlar.
/// Bu yüzden çeviri Internship tarafında, aktarıcı (relay) tüketicilerde yapılır.</para>
///
/// <para><b>Karar burada saf tutuldu</b>, tüketiciler yalnız veri getirir: hangi saga'nın
/// aday olduğu bir domain kuralıdır ve testle kilitlenebilmelidir.</para>
/// </summary>
public static class SagaCorrelationPolicy
{
    /// <summary>
    /// Sözleşme olayları için aday saga: <b>öğrenci + işletme</b>. Sözleşme akademik dönem
    /// taşımıyor; öğrencinin aynı işletmede aynı anda iki açık stajı olamayacağı için bu ikili
    /// yeterlidir.
    /// </summary>
    public static bool MatchesContract(
        Guid sagaStudentId, Guid? sagaBusinessId, InternshipPhase phase,
        Guid studentId, Guid businessId)
        => sagaStudentId == studentId && sagaBusinessId == businessId && IsOpen(phase);

    /// <summary>
    /// Devamsızlık olayları için aday saga: <b>öğrenci + akademik dönem</b>. İşletme
    /// KULLANILMAZ — devamsızlık sayacı da öğrencinin eğitim yılına aittir, işletmeye değil
    /// (#242); öğrenci yıl içinde işletme değiştirmiş olabilir ve saga aynı saga'dır.
    /// </summary>
    public static bool MatchesAttendance(
        Guid sagaStudentId, Guid sagaAcademicPeriodId, InternshipPhase phase,
        Guid studentId, Guid academicPeriodId)
        => sagaStudentId == studentId && sagaAcademicPeriodId == academicPeriodId && IsOpen(phase);

    /// <summary>
    /// Kapanmış staja olay taşınmaz. <c>TerminationInProgress</c> <b>açık sayılır</b>: fesih
    /// süreci sürerken gelen sözleşme feshi olayı zincirin devamıdır, yenisi değil.
    /// </summary>
    public static bool IsOpen(InternshipPhase phase)
        => phase != InternshipPhase.Terminated && phase != InternshipPhase.Completed;
}
