namespace MESNET.Attendance.Core.ReadModels;

/// <summary>
/// Öğrenci ad/numara araması için lokal denormalize read-model.
/// Enrollment.StudentRegistered event'i ile beslenir (cross-module isim çözümlemesi).
/// </summary>
public class StudentNameView
{
    public Guid Id { get; set; } // = StudentId
    public string FullName { get; set; } = default!;
    public string? StudentNumber { get; set; }

    /// <summary>Öğrencinin alan (branş) kodu — devamsızlık listesinde server-side alan filtresi için.</summary>
    public string? BranchCode { get; set; }
}
