namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Dönem Not Fişi: "İşletmelerde Meslek Eğitimi Gören Öğrencilere Ait Dönem Not Fişi".
/// MEB Mesleki ve Teknik Eğitim Yönetmeliği md. 82. İşletmede verilen dönem puanları
/// (Temrin/İş-Hizmet/Proje/Deney) + okulda verilen (*) puanlar + dönem ortalaması.
/// </summary>
public sealed class TermGradeSlipFormData
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    // İlişkili entity ID'leri (GeneratedDocument'a kopyalanır)
    public Guid? StudentId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    // Okul / dönem bilgileri
    public required string InstitutionName { get; init; }   // Okul/Kurumun Adı
    public required string AcademicYear { get; init; }      // Öğretim Yılı  ör. "2025 / 2026"
    public required string Semester { get; init; }          // Dönemi        ör. "2. Dönem"
    public string CourseName { get; init; } = "İşletmede Beceri Eğitimi"; // Ders

    // İşletme
    public required string BusinessName { get; init; }      // İşletmenin Adı
    public string? BusinessPhone { get; init; }             // Tel
    public string? BusinessEmail { get; init; }             // E-Posta

    // Öğrenci
    public required string StudentNumber { get; init; }     // Numarası
    public required string StudentFullName { get; init; }   // Adı Soyadı
    public required string BranchName { get; init; }        // Meslek Alan/Dalı

    // İşletmede verilen puanlar — her kategori dönem boyunca birden çok not içerebilir
    public List<int> PracticeGrades { get; init; } = [];    // Temrin
    public List<int> ServiceGrades { get; init; } = [];     // İş-Hizmet
    public List<int> ProjectGrades { get; init; } = [];     // Proje
    public List<int> ExperimentGrades { get; init; } = [];  // Deney

    // Okulda verilen puanlar — (*) okul/kurum müdürlüğünce doldurulur (md. 82)
    public int? MakeupTrainingScore { get; init; }          // Telafi Eğitim Puanı (*)
    public int? SkillCompetitionScore { get; init; }        // Beceri Yarışması Puanı (*)

    // Dönem başarısı
    public decimal? TermAverage { get; init; }              // Dönem Puanı Ortalaması (rakam ile)
    public string? TermAverageInWords { get; init; }        // Dönem Puanı Ortalaması (yazı ile)

    // İmza blokları (ad opsiyonel — boşsa yalnız ünvan + imza satırı)
    public string? MasterInstructorName { get; init; }      // Usta Öğretici / Eğitici Personel
    public string? BusinessOfficialName { get; init; }      // İşletme Yetkilisi
    public string? VicePrincipalName { get; init; }         // Okul/Kurum Koor. Müdür Yardımcısı
    public string? PrincipalName { get; init; }             // Okul/Kurum Müdürü
}
