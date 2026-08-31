namespace MESNET.Internship.Application.Dtos;

/// <param name="TotalCount">Alt ağaçtaki tıkanmış zincir sayısı.</param>
/// <param name="ThresholdDays">Karar anındaki eşik — ön yüz boş-durum metnini bununla yazar.</param>
public sealed record StuckApprovalSummaryDto(
    int TotalCount,
    int ThresholdDays,
    IReadOnlyList<StuckApprovalByInstitutionDto> ByInstitution);

/// <param name="InstitutionName">
/// <b>Her zaman <c>null</c></b>: kurum adı Institution modülünündür ve buradan okunamaz (şema
/// izolasyonu). Ön yüz lookup map ile doldurur. Alan yine de durur ki istemci kendi tipini
/// uydurmasın.
/// </param>
/// <param name="OldestDays">
/// En eski zincirin yaşı. <c>null</c> = o kurumdaki tıkanmış zincirlerin hiçbirinde talep
/// zamanı bilinmiyor. Sıfır ya da sentinel yazmak sayıyı sessizce yanlışlardı.
/// </param>
public sealed record StuckApprovalByInstitutionDto(
    Guid InstitutionId,
    string? InstitutionName,
    int Count,
    int? OldestDays);
