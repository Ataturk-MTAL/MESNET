namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 1: İşletmelerde Beceri Eğitimi Sözleşmesi verileri (5 sayfa)
/// </summary>
public sealed class InternshipContractFormData
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    // İlişkili entity ID'leri (GeneratedDocument'a kopyalanır)
    public Guid? StudentId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? InstitutionId { get; init; }
    public Guid? TeacherId { get; init; }

    // Kurum bilgileri
    public required string InstitutionName { get; init; }
    public required string InstitutionAddress { get; init; }
    public required string InstitutionPhone { get; init; }
    public required string InstitutionCity { get; init; }
    public required string InstitutionDistrict { get; init; }
    public required string PrincipalName { get; init; }

    // İşletme bilgileri
    public required string BusinessName { get; init; }
    public required string BusinessAddress { get; init; }
    public required string BusinessPhone { get; init; }
    public string? BusinessEmail { get; init; }
    public required string TaxNumber { get; init; }
    public required string SgkNumber { get; init; }
    public required string ActivityField { get; init; }

    // Usta öğretici bilgileri
    public required string MasterInstructorName { get; init; }
    public required string MasterInstructorTcKimlik { get; init; }
    public required string MasterInstructorCertificateNo { get; init; }
    public required string MasterInstructorCertificateType { get; init; }

    // Öğrenci bilgileri
    public required string StudentFullName { get; init; }
    public required string StudentTcKimlik { get; init; }
    public required string StudentNumber { get; init; }
    public DateTime StudentBirthDate { get; init; }
    public required string StudentBirthPlace { get; init; }
    public required string FatherName { get; init; }
    public required string MotherName { get; init; }
    public required string BranchName { get; init; }
    public int ClassYear { get; init; }

    // Veli bilgileri
    public required string GuardianName { get; init; }
    public required string GuardianTcKimlik { get; init; }
    public required string GuardianPhone { get; init; }
    public required string GuardianAddress { get; init; }
    public required string GuardianRelationship { get; init; }

    // Sözleşme bilgileri
    public required string AcademicYear { get; init; }
    public DateTime ContractStartDate { get; init; }
    public DateTime ContractEndDate { get; init; }
    public required string CoordinatorTeacherName { get; init; }

    // İmza bilgileri
    public DateTime? InstitutionSignedAt { get; init; }
    public DateTime? BusinessSignedAt { get; init; }
    public DateTime? StudentSignedAt { get; init; }
    public DateTime? ParentSignedAt { get; init; }
}
