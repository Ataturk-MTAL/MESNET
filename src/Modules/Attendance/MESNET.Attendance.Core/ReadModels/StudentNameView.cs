namespace MESNET.Attendance.Core.ReadModels;

/// <summary>
/// Öğrenci ad/numara araması için lokal denormalize read-model.
/// Enrollment.StudentRegistered event'i ile beslenir (cross-module isim çözümlemesi).
/// </summary>
public class StudentNameView
{
    public Guid Id { get; set; } // = StudentId

    /// <summary>
    /// Kiracı anahtarı (#147). Türetilmiş görünüm olsa da kiracıya ait veri taşır; anahtarsız
    /// hâlinde çok-okul sorgusu iki okulun satırını ayırt edemezdi.
    /// </summary>
    public Guid InstitutionId { get; set; }
    public string FullName { get; set; } = default!;
    public string? StudentNumber { get; set; }

    /// <summary>Öğrencinin alan (branş) kodu — devamsızlık listesinde server-side alan filtresi için.</summary>
    public string? BranchCode { get; set; }

    /// <summary>
    /// <c>Formal</c> (örgün) veya <c>Mesem</c> (#175). Ücretli izin hakkı yalnız MESEM'dedir;
    /// tür doğrulaması bu alandan yapılır ve modüller arası doğrudan sorgu yasak olduğu için
    /// burada denormalize tutulur.
    ///
    /// <para>Alan #175 ile eklendi; daha önce yazılmış satırlarda <c>null</c>'dur ve o
    /// öğrencilerde ücretli izin girişi reddedilir.
    /// <c>POST /api/enrollment/students/resync-projections</c> ile doldurulur.</para>
    /// </summary>
    public string? EducationType { get; set; }
}
