using System.Security.Cryptography;
using System.Text;

namespace MESNET.Coordination.Core.ReadModels;

/// <summary>
/// Enrollment modülünden gelen event'lerle güncellenen, alan bazlı öğrenci sayısı read model'i.
/// Deterministic ID: aynı (InstitutionId, PeriodId, BranchCode, EducationType) kombinasyonu → aynı Guid.
/// </summary>
public sealed class BranchStudentCountView
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string EducationType { get; set; } = "Formal";

    /// <summary>Sınıf bazında öğrenci sayıları: ClassYear → Count</summary>
    public Dictionary<int, int> StudentCountByClassYear { get; set; } = new();

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Deterministic GUID üretir — aynı kombinasyon her zaman aynı ID'yi döndürür.
    /// </summary>
    public static Guid CreateId(Guid institutionId, Guid academicPeriodId, string branchCode, string educationType)
    {
        var key = $"{institutionId}:{academicPeriodId}:{branchCode}:{educationType}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }
}
