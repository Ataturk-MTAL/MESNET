namespace MESNET.Internship.Application.Dtos;

/// <summary>
/// Fesih onay zincirinin arayüze açılan durumu (#191).
///
/// <para><b>Neden yeni bir DTO:</b> <see cref="TerminationApprovalChainDto"/> yalnız ham
/// bayrakları taşıyor ve hiçbir uçtan dönmüyordu (ölü kod). Arayüzün "hangi adım bekliyor,
/// bu kullanıcı hangisini yapabilir" sorularını yanıtlaması için bayraklar tek başına yetmez —
/// veli onayının aranıp aranmadığı ve adımların izinleri de gerekir.</para>
/// </summary>
/// <param name="IsActive">
/// Fesih süreci açık mı. <c>false</c> ise zincir hiç başlamamıştır ve
/// <paramref name="Chain"/> <c>null</c>'dur.
/// </param>
/// <param name="Chain">Ham onay bayrakları; süreç açılmamışsa <c>null</c>.</param>
/// <param name="NextStep">
/// Sıradaki adım — zincir <b>sıralıdır</b> (#218), aynı anda yalnız bir adım onaylanabilir.
/// Zincir kapandıysa ya da override edildiyse <c>null</c>.
/// </param>
/// <param name="TerminationReason">Fesih gerekçesi (talep sırasında girilir).</param>
/// <param name="TerminationReasonType">Fesih gerekçe türü — talebi kimin açtığını da taşır.</param>
public sealed record TerminationChainStatusDto(
    bool IsActive,
    TerminationApprovalChainDto? Chain,
    TerminationStepDto? NextStep,
    string? TerminationReason,
    string? TerminationReasonType);

/// <summary>
/// Zincirin tek bir adımı — arayüz butonu bu bilgiyle kurulur.
/// </summary>
/// <param name="Name">İngilizce ad (<c>Teacher</c>, <c>Deputy</c>, <c>Director</c>).</param>
/// <param name="Slug">Türkçe görünen ad.</param>
/// <param name="Endpoint">
/// <c>POST /api/internships/{id}/approve/{Endpoint}</c> yolundaki son parça.
/// </param>
/// <param name="Permission">
/// Adımı yapabilmek için gereken izin. Arayüz butonu buna bakar; rol adına DEĞİL (ADR-0001).
/// </param>
public sealed record TerminationStepDto(
    string Name,
    string Slug,
    string Endpoint,
    string Permission);
