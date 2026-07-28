namespace MESNET.Coordination.Application.Dtos;

// ── Teacher Schedule ──

public sealed record TeacherScheduleDto(
    Guid Id,
    Guid TeacherId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int AcademicYear,
    string Semester,
    List<DailyScheduleDto> WeeklySchedule,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    // Kimlik saklanır, ad okuma anında UserNameView'dan çözülür (#137).
    // Ad bilinmiyorsa null — hata değil (silinmiş kullanıcı / backfill henüz koşmamış).
    Guid CreatedById,
    string? CreatedByName,
    int Version);

public sealed record DailyScheduleDto(
    string Day,
    List<PeriodSlotDto> Periods);

public sealed record PeriodSlotDto(
    int PeriodNumber,
    string Status,
    string? CourseName,
    Guid? AssignedBusinessId);

// ── Schedule History ──

/// <summary>
/// Bir öğretmenin tüm ders programı stream'lerinin özeti
/// </summary>
public sealed record ScheduleStreamSummaryDto(
    Guid ScheduleId,
    int AcademicYear,
    string Semester,
    int VersionCount,
    DateTime CreatedAt,
    DateTime? LastUpdatedAt,
    // Bkz. TeacherScheduleDto — kimlik saklanır, ad okuma anında çözülür (#137).
    Guid CreatedById,
    string? CreatedByName,
    Guid? LastUpdatedById,
    string? LastUpdatedByName);

/// <summary>
/// Bir ders programının değişiklik geçmişi (event listesi)
/// </summary>
public sealed record ScheduleVersionDto(
    int Version,
    string EventType,
    DateTime Timestamp,
    Guid UpdatedById,
    string? UpdatedByName,
    List<DailyScheduleDto> WeeklySchedule);

public sealed record ScheduleHistoryDto(
    Guid ScheduleId,
    Guid TeacherId,
    int AcademicYear,
    string Semester,
    int CurrentVersion,
    List<ScheduleVersionDto> Versions);

// ── Business Assignment ──

/// <param name="IsHonoraryVisit">
/// Fahri (ücretsiz) ziyaret (#115). True ise <paramref name="AssignedHours"/> her zaman 0'dır
/// ve satır havuz/öğretmen kapasitesi toplamlarına girmez. False + 0 saat = "henüz takdir edilmedi".
/// </param>
public sealed record BusinessAssignmentDto(
    Guid BusinessId,
    string BusinessName,
    string? Address,
    string? District,
    double? DistanceToSchoolKm,
    bool IsManualDistance,
    int MaxCoordinationHours,
    int AssignedHours,
    bool IsHonoraryVisit,
    Guid? AssignedTeacherId,
    string? AssignedTeacherName,
    string? AssignedDay,
    int? AssignedPeriodNumber,
    int ActiveStudentCount,
    string BranchCode,
    string BranchName,
    List<AssignedSlotInfoDto> AssignedSlots,
    DateTime? LastModifiedAt = null,
    Guid? LastModifiedById = null,
    string? LastModifiedByName = null);

public sealed record AssignmentHistoryEntryDto(
    DateTime Timestamp,
    string Action,
    Guid PerformedById,
    string? PerformedByName,
    string? TeacherName,
    string? SlotDay,
    int? SlotPeriod,
    int? AssignedHours,
    string? Details);

public sealed record AssignedSlotInfoDto(string Day, int PeriodNumber);

/// <param name="TotalAssignedHours">
/// Ücret doğuran toplam saat — fahri ziyaretler dahil DEĞİLDİR (#115).
/// </param>
/// <param name="HonoraryBusinessCount">Havuza girmeyen fahri ziyaret satırı sayısı.</param>
public sealed record CoordinationSummaryDto(
    int TotalWorkloadPool,
    int TotalAssignedHours,
    int RemainingHours,
    int TotalMaxHours,
    int AssignedBusinessCount,
    int UnassignedBusinessCount,
    int HonoraryBusinessCount,
    List<TeacherWorkloadSummaryDto> TeacherWorkloads);

/// <param name="AssignedHours">Ücret doğuran saat — fahri ziyaretler hariç.</param>
/// <param name="HonoraryVisitCount">
/// Öğretmenin fahri ziyaret ettiği işletme sayısı. Ders programında slot işgal eder,
/// ek ders saatine sayılmaz — ayrı gösterilmezse öğretmen yükü olduğundan az görünür.
/// </param>
public sealed record TeacherWorkloadSummaryDto(
    Guid TeacherId,
    string TeacherName,
    int AssignedHours,
    int BusinessCount,
    int HonoraryVisitCount);

public sealed record TeacherWorkloadDto(
    Guid TeacherId,
    int TotalAssignedHours,
    int BusinessCount,
    int HonoraryVisitCount,
    List<TeacherBusinessAssignmentDto> Businesses);

public sealed record TeacherBusinessAssignmentDto(
    Guid BusinessId,
    string BusinessName,
    int AssignedHours,
    string? AssignedDay,
    bool IsHonoraryVisit);

// ── Teacher Overview ──

/// <summary>
/// Tek öğretmenin iş yükü + boş saat özeti (öğretmen sekmesi için)
/// </summary>
public sealed record TeacherOverviewDto(
    Guid TeacherId,
    int TotalAssignedHours,
    int BusinessCount,
    int HonoraryVisitCount,
    bool ScheduleExists,
    Dictionary<string, int> FreeSlotsByDay,      // gün → boş slot sayısı
    Dictionary<string, int> TotalSlotsByDay,     // gün → toplam serbest slot
    List<TeacherBusinessAssignmentDto> Businesses);

/// <summary>
/// Tüm öğretmenlerin tek satır özeti (öğretmen sekmesi tablosu için)
/// </summary>
/// <param name="AssignedHours">Ücret doğuran saat — fahri ziyaretler hariç (#115).</param>
/// <param name="HonoraryVisitCount">Fahri ziyaret edilen işletme sayısı.</param>
/// <param name="HonorarySlotCount">
/// Fahri ziyaretlerin ders programında işgal ettiği slot sayısı. "Atanan Saat" ile
/// programdaki dolu slot sayısı arasındaki farkı bu değer açıklar.
/// </param>
public sealed record TeacherSummaryRowDto(
    Guid TeacherId,
    string TeacherName,
    int BusinessCount,
    int AssignedHours,
    int HonoraryVisitCount,
    int HonorarySlotCount,
    bool ScheduleExists,
    Dictionary<string, int> FreeSlotsByDay,       // gün → boş slot sayısı
    Dictionary<string, int> AssignedSlotsByDay);   // gün → atanmış koordinatörlük slot sayısı

// ── Weekly Visit ──

public sealed record WeeklyVisitPlanDto(
    Guid Id,
    Guid AcademicPeriodId,
    int Year,
    int WeekNumber,
    string WeekStartDate,
    string WeekEndDate,
    string Scope,
    Guid? ScopeTeacherId,
    string? ScopeBranchCode,
    int AssignmentCount,
    string GeneratedBy,
    DateTime GeneratedAt);

public sealed record WeeklyVisitAssignmentDto(
    Guid Id,
    Guid PlanId,
    Guid TeacherId,
    string TeacherName,
    Guid BusinessId,
    string BusinessName,
    string BranchCode,
    string BranchName,
    string VisitDate,
    string Day,
    int PeriodCount,
    int WeekNumber);

/// <summary>
/// İşletme kümeleme noktası (harita için)
/// </summary>
public sealed record BusinessClusterDto(
    Guid BusinessId,
    string BusinessName,
    double Latitude,
    double Longitude,
    string? District,
    string BranchCode,
    string BranchName,
    int? ClusterId,                // null = gürültü (outlier)
    string? AssignedTeacherName,
    bool IsAssigned,
    int ActiveStudentCount,
    double? DistanceToSchoolKm,
    int MaxCoordinationHours,      // mesafe formülünden hesaplanan maks saat
    bool IsHonoraryVisit);         // fahri (ücretsiz) ziyaret — saat takdiri yapılmaz (#115)

// ── Saat Dağıtım Önerisi (#116) ──

/// <summary>
/// Öneri satırı. Ekranda satır başına bir kova rozeti ve "neden bu kadar saat"
/// açıklaması bu kayıttan üretilir.
/// </summary>
/// <param name="Weight">
/// <c>w = MaxHours × StudentCount</c> — dağıtım sırasının tek satırlık gerekçesi.
/// </param>
/// <param name="SuggestedHours">Önerilen saat; fahri satırlarda 0.</param>
/// <param name="IsPinned">Koordinatör bu satırı kilitledi — öneri değeri değiştirmedi.</param>
/// <param name="Bucket">Kova adı (İngilizce, <c>AllocationBucket.Name</c>) — programatik ayrım.</param>
/// <param name="BucketLabel">
/// Kovanın Türkçe etiketi (<c>AllocationBucket.Slug</c>). Ayrım yalnız renkle değil
/// metinle de taşınsın diye rozette basılır (renk körlüğü).
/// </param>
public sealed record HoursSuggestionLineDto(
    Guid BusinessId,
    string BusinessName,
    string BranchCode,
    int MaxHours,
    int StudentCount,
    long Weight,
    int SuggestedHours,
    bool IsPinned,
    bool IsHonoraryVisit,
    string Bucket,
    string BucketLabel);

/// <summary>
/// Öneriyle birlikte dönen tanılama — "havuz nereye gitti" sorusunun ekrandaki cevabı.
/// Hiçbir artık sessizce yutulmaz.
/// </summary>
/// <param name="Pool">Ders yükü havuzu (<c>P</c>).</param>
/// <param name="TeacherCapacity">Alan öğretmenlerinin kalan kapasitesi (<c>C</c>).</param>
/// <param name="SumOfMax">Σ max_i — tüm işletmeler tavanına çıksa gereken saat.</param>
/// <param name="TotalAllocated">Σ önerilen saat (kilitli satırlar dahil).</param>
/// <param name="Undistributed">
/// <c>P − TotalAllocated</c>. Pozitif → dağıtılamayan havuz artığı;
/// negatif → kilitli satırların toplamı havuzu aşıyor.
/// </param>
/// <param name="HonoraryCount">Fahri kovasındaki işletme sayısı.</param>
/// <param name="OutOfBranchHours">Alan dışı öğretmene önerilen toplam saat.</param>
/// <param name="IsPoolUndefined">Havuz hesaplanmamış (<c>P ≤ 0</c>) — öneri üretilmedi.</param>
public sealed record HoursSuggestionDiagnosticsDto(
    int Pool,
    int TeacherCapacity,
    int SumOfMax,
    int TotalAllocated,
    int Undistributed,
    int HonoraryCount,
    int OutOfBranchHours,
    bool IsPoolUndefined);

/// <summary>
/// Saat dağıtım önerisinin tamamı. Satırlar ağırlık sırasındadır
/// (ağırlık ↓, tavan ↓, alan kodu ↑, kimlik ↑) — deterministiktir.
/// </summary>
public sealed record HoursSuggestionDto(
    List<HoursSuggestionLineDto> Lines,
    HoursSuggestionDiagnosticsDto Diagnostics);
