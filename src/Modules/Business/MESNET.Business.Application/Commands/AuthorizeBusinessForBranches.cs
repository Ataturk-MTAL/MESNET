namespace MESNET.Business.Application.Commands;

/// <summary>İdarenin işaretlediği tek bir alan + dayanak belgesi.</summary>
public sealed record BranchAuthorizationItem(string BranchCode, Guid? BasedOnDocumentId = null);

/// <summary>
/// İdare, belge incelemesi sonucunda işletmenin hangi alanlardan öğrenci alabileceğini işaretler (#119).
///
/// YERİNE KOYMA semantiği: <paramref name="Branches"/> işletmenin nihai aktif alan listesidir.
/// Listede olmayan mevcut yetkiler iptal edilir (kayıt silinmez, RevokedAt damgalanır).
/// Boş liste gönderilirse işletme hiçbir alandan öğrenci alamaz.
/// </summary>
public sealed record AuthorizeBusinessForBranches(
    Guid BusinessId,
    List<BranchAuthorizationItem> Branches);
