namespace MESNET.Security.Application.Commands;

/// <summary>
/// Aktörün aktif bağlamını değiştirir (B parçası).
/// </summary>
/// <param name="InstitutionId">
/// Adına davranılacak kurum; <c>null</c> bağlamı temizler ve aktörü ev kurumuna döndürür.
/// </param>
/// <remarks>
/// <b>Hedef aktörün KENDİ kaydından okunmaz, istekten gelir</b> — ama yetki değil NİYET
/// olarak. Sunucu hedefin aktörün alt ağacında olduğunu doğrular; değilse <c>DomainException</c>.
/// Aynı ayrım <c>IInstitutionScoped</c> uçlarında da var: kimlik istekten, karar sunucudan.
///
/// <para>Komut <c>Commands/</c> altındadır, dolayısıyla denetim izine kendiliğinden düşer
/// (C parçası). Ayrı bir kayıt yolu yazılmaz.</para>
/// </remarks>
public sealed record SetActiveInstitution(Guid? InstitutionId);
