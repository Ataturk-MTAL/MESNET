using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Entities;

/// <summary>
/// Öğrencinin işletmede dönem boyunca aldığı notlar — Dönem Not Fişi'nin (MEB md. 82) kaynağı.
/// İşletme yetkilisi (CompanyManager) not giriş penceresinde işletme notlarını girer;
/// okul-payı (*) alanlarını ve kesinleştirmeyi okul/koordinatör yapar.
/// </summary>
public sealed class StudentTermGrade
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public Guid? TeacherId { get; set; }

    // Yerleştirmeden denormalize (fiş + liste için) — kaynak: Enrollment.StudentPlaced
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;

    // İşletmenin doldurduğu — usta öğretici adı (ayrı login değil)
    public string? MasterInstructorName { get; set; }

    // İşletmede verilen puanlar (her kategori birden çok not)
    public List<int> PracticeGrades { get; set; } = [];    // Temrin
    public List<int> ServiceGrades { get; set; } = [];     // İş-Hizmet
    public List<int> ProjectGrades { get; set; } = [];     // Proje
    public List<int> ExperimentGrades { get; set; } = [];  // Deney

    // Okul-payı (*) — okul/kurum müdürlüğünce doldurulur
    public int? MakeupTrainingScore { get; set; }          // Telafi Eğitim Puanı (*)
    public int? SkillCompetitionScore { get; set; }        // Beceri Yarışması Puanı (*)

    // Otomatik hesaplanan dönem ortalaması
    public decimal? TermAverage { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<StudentTermGradeStatus, int>))]
    public StudentTermGradeStatus Status { get; set; } = StudentTermGradeStatus.Draft;

    // Düz string kopya — Marten LINQ filtreleri için (SmartEnum LINQ tuzağı; bkz. CLAUDE.md)
    public string StatusName { get; set; } = StudentTermGradeStatus.Draft.Name;

    public string? EnteredByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
