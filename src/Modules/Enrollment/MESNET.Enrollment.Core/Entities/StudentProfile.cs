using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Common.Shared.Enums;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.ValueObjects;

namespace MESNET.Enrollment.Core.Entities;

public class StudentProfile
{
    public Guid Id { get; set; }
    public Guid KeycloakUserId { get; set; }
    public required string FullName { get; set; }
    public string? TcKimlikNo { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? BirthPlace { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? StudentNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public required string BranchCode { get; set; }
    public required string BranchName { get; set; }
    public string? SpecializationCode { get; set; }
    public string? SpecializationName { get; set; }
    public int ClassYear { get; set; }
    public string? Section { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<EducationType, int>))]
    public EducationType EducationType { get; set; } = EducationType.Formal;

    /// <summary>
    /// Kalfalık yeterliğini kazandı mı. 3308 Madde 25: asgari ücretin %50'si oranı yalnız
    /// "kalfalık yeterliğini kazanan mesleki eğitim merkezi 12'nci sınıf öğrencileri" için
    /// geçerlidir. Yeterliği olmayan MESEM öğrencisi işletme büyüklüğü oranına (%15/%30) tabidir.
    /// Varsayılan false — eksik veri fazla ödeme üretmesin (#83).
    /// </summary>
    public bool HasJourneymanQualification { get; set; }

    /// <summary>Öğrenci / Aday Çırak / Çırak — ücret tabanını belirler (#85).</summary>
    [JsonConverter(typeof(SmartEnumNameConverter<StudentCategory, int>))]
    public StudentCategory Category { get; set; } = StudentCategory.Student;

    /// <summary>SmartEnum LINQ tuzağı: sorgular için düz string kopya.</summary>
    public string CategoryName { get; set; } = StudentCategory.Student.Name;

    /// <summary>LINQ sorguları için düz string kopyası</summary>
    public string EducationTypeName { get; set; } = EducationType.Formal.Name;

    private StudentStatus _status = StudentStatus.Registered;

    [JsonConverter(typeof(SmartEnumNameConverter<StudentStatus, int>))]
    public StudentStatus Status
    {
        get => _status;
        set { _status = value; StatusName = value.Name; }
    }

    /// <summary>LINQ sorguları için düz string kopyası — Status setter'ı otomatik senkron tutar
    /// (SmartEnum nested-path LINQ tuzağı: s.Status.Name NULL döner).</summary>
    public string StatusName { get; private set; } = StudentStatus.Registered.Name;

    public GuardianInfo? Guardian { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
