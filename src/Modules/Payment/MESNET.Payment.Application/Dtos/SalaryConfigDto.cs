namespace MESNET.Payment.Application.Dtos;

/// <summary>
/// Bir yürürlük döneminin asgari ücret ve hesaplama parametreleri.
/// </summary>
public sealed record SalaryConfigDto(
    Guid Id,
    decimal MinimumWage,
    decimal? MinimumWageUnder16,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    /// <summary>Bugün yürürlükte olan dönem mi.</summary>
    bool IsCurrent,
    /// <summary>Yürürlüğü henüz başlamamış (ileri tarihli) dönem mi.</summary>
    bool IsScheduled,
    Guid UpdatedById,
    /// <summary>Son değişikliği yapan kullanıcının adı — <c>UserNameView</c>'dan çözülür (#137).</summary>
    string? UpdatedBy,
    decimal SmallBusinessRate,
    decimal LargeBusinessRate,
    int PersonnelThreshold,
    decimal ApprenticeRate,
    decimal MEM12thGradeRate,
    decimal GovContribSmallNonMEM,
    decimal GovContribLargeNonMEM,
    decimal GovContribMEM);

/// <summary>
/// Wolverine, handler'dan dönen <c>IEnumerable&lt;T&gt;</c>'i cascading message sayar ve
/// koleksiyonu çağırana DÖNDÜRMEZ — bu yüzden liste somut bir DTO'ya sarılır.
/// </summary>
public sealed record SalaryConfigHistoryDto(List<SalaryConfigDto> Items);
