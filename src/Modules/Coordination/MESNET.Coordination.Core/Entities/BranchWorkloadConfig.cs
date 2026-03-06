namespace MESNET.Coordination.Core.Entities;

/// <summary>
/// Alan bazlı koordinatörlük ders yükü yapılandırması.
/// Şeflik saatleri + sınıf bazlı grup sayısı × haftalık ders saati → toplam havuz.
/// Norm Kadro Yönetmeliği Madde 22 ve Mesleki Eğitim Yönetmeliği referanslıdır.
/// </summary>
public sealed class BranchWorkloadConfig
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }
    public Guid AcademicPeriodId { get; set; }
    public string BranchCode { get; set; } = string.Empty;

    /// <summary>"Formal" veya "Mesem" — farklı grup tabloları</summary>
    public string EducationType { get; set; } = "Formal";

    // ── Şeflik ──
    public int DepartmentHeadCount { get; set; }
    public int WorkshopHeadCount { get; set; }
    public int DepartmentHeadHours { get; set; } = 10;
    public int WorkshopHeadHours { get; set; } = 6;

    // ── Sınıf bazlı konfigürasyon ──
    public List<ClassLevelConfig> ClassLevels { get; set; } = [];

    // ── Hesaplanan değerler ──

    /// <summary>Şeflik toplamı = (alanŞefi × saati) + (atölyeŞefi × saati)</summary>
    public int TotalSupervisorHours { get; set; }

    /// <summary>Ders yükü toplamı = Σ(sınıf bazında haftalıkDers × grupSayısı)</summary>
    public int TotalTeachingHours { get; set; }

    /// <summary>Toplam havuz = TotalTeachingHours + TotalSupervisorHours</summary>
    public int TotalWorkloadPool { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>Hesaplanan değerleri günceller.</summary>
    public void Recalculate()
    {
        TotalSupervisorHours = (DepartmentHeadCount * DepartmentHeadHours)
                             + (WorkshopHeadCount * WorkshopHeadHours);

        TotalTeachingHours = ClassLevels.Sum(c => c.SubTotal);
        TotalWorkloadPool = TotalTeachingHours + TotalSupervisorHours;
    }
}

/// <summary>
/// Tek bir sınıf seviyesi için ders yükü konfigürasyonu.
/// </summary>
public sealed record ClassLevelConfig
{
    /// <summary>Sınıf seviyesi (9, 10, 11, 12)</summary>
    public int ClassYear { get; init; }

    /// <summary>Haftalık ders saati (çerçeve programına göre, elle girilen)</summary>
    public int WeeklyLessonHours { get; init; }

    /// <summary>İşletmeye giden öğrenci sayısı (Enrollment'tan veya elle)</summary>
    public int StudentCount { get; init; }

    /// <summary>Otomatik hesaplanan grup sayısı (Madde 22)</summary>
    public int GroupCount { get; init; }

    /// <summary>Alt toplam = WeeklyLessonHours × GroupCount</summary>
    public int SubTotal { get; init; }
}
