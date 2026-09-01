using System.Security.Cryptography;
using System.Text;

namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// Öğrenci yerleştirme bilgileri — Enrollment ve Business modülü event'lerinden oluşturulur.
/// Aylık devamsızlık formunda öğrenci, işletme ve alan bilgisi için kullanılır.
/// </summary>
public class StudentPlacementReportView
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int ClassYear { get; set; }
    /// <summary>İşletme — okulda stajda null (#159); raporda "Okulda" olarak gösterilir.</summary>
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string? BusinessPhone { get; set; }
    public string? BusinessEmail { get; set; }
    public string? BusinessContactName { get; set; }
    public string BranchCode { get; set; } = "";
    public string BranchName { get; set; } = "";
    public Guid? TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    /// <summary>
    /// Satırın DETERMİNİSTİK kimliği: <c>(StudentId, AcademicPeriodId)</c> (#296).
    ///
    /// <para><b>Neden gerekti:</b> kimlik iki ayrı yoldan, iki ayrı biçimde üretiliyordu —
    /// öğrenci yolunda <c>Id = StudentId</c>, yerleştirme yolunda <c>Id = PlacementId</c>. Hangi
    /// kimliğin kullanıldığı hangi olayın ÖNCE geldiğine bağlıydı; iki yol da satırı sorguyla
    /// bulduğu için sapma görünmüyordu.</para>
    ///
    /// <para><b>Asıl kusur <c>Id = StudentId</c>'ydi:</b> satırın mantıksal anahtarı
    /// (öğrenci, akademik dönem) ikilisidir — okuyan her sorgu ikisiyle birden süzüyor. Öğrenci
    /// kimliğini anahtar yapmak, öğrencinin <b>ikinci akademik döneminin</b> birincisini ezmesi
    /// demekti; ölçüldü (dev, 01.09.2026): 363 satırın 363'ünde <c>Id == StudentId</c> ve henüz
    /// tek dönem olduğu için kayıp görünmüyordu — hata gelecek öğretim yılında doğacaktı.</para>
    ///
    /// <para><c>BranchStudentCountView.CreateId</c> ile aynı teknik.</para>
    /// </summary>
    public static Guid CreateId(Guid studentId, Guid academicPeriodId)
    {
        var key = $"{studentId}:{academicPeriodId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>
    /// Satırın <b>tamamını</b> yeni kimlikle kopyalar — eski kimlikli satırdan göç için (#296).
    ///
    /// <para>Alan listesi elle tutulur ve <c>PlacementReportViewCopyTests</c> ile kilitlenir:
    /// yansımayla her yazılabilir alan karşılaştırılır. Elle liste, unutulan alanın tam olarak
    /// tekrar unutulacağı yerdir — #297'de <c>WithId</c> bir alanı düşürmüştü.</para>
    /// </summary>
    public StudentPlacementReportView WithId(Guid id) => new()
    {
        Id = id,
        StudentId = StudentId,
        StudentName = StudentName,
        StudentNumber = StudentNumber,
        ClassName = ClassName,
        ClassYear = ClassYear,
        BusinessId = BusinessId,
        BusinessName = BusinessName,
        BusinessPhone = BusinessPhone,
        BusinessEmail = BusinessEmail,
        BusinessContactName = BusinessContactName,
        BranchCode = BranchCode,
        BranchName = BranchName,
        TeacherId = TeacherId,
        TeacherName = TeacherName,
        InstitutionId = InstitutionId,
        AcademicPeriodId = AcademicPeriodId,
    };
}
