namespace MESNET.Contract.Core.ReadModels;

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
}
