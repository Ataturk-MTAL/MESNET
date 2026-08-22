namespace MESNET.Coordination.Core.ReadModels;

/// <summary>
/// <b>Okulda staj</b> yapan öğrenci — işverensiz yerleştirme (#159, <c>StudentPlaced</c> olayında
/// <c>BusinessId = null</c>). Okul tarafının dönem notu girişini besler (#171).
///
/// <para><b>Neden ayrı görünüm:</b> <see cref="CoordinationPlacedStudentView"/> işletme
/// kapsamlıdır — koordinasyon ekranları ziyaret edilecek işletmeyi listeler ve
/// <c>BusinessId</c> alanı zorunludur. Oraya işletmesiz satır eklemek o görünümün anlamını
/// bozardı; kapsamı işletme olan her sorgu bu satırları yanlışlıkla toplardı.</para>
///
/// <para><c>TeacherId</c> okulda stajda <b>gözetmen</b>dir (alan/atölye şefi olabilir), ziyaret
/// yapan koordinatör değil.</para>
/// </summary>
public class SchoolPlacedStudentView
{
    public Guid Id { get; set; }            // PlacementId
    public Guid StudentId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public Guid? TeacherId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
