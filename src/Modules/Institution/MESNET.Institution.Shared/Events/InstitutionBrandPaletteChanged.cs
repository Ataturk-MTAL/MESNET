namespace MESNET.Institution.Shared.Events;

/// <summary>
/// Kurum marka paletini değiştirdi. Hex'ler olaya <b>çözümlenmiş hâlde</b> konur ki dinleyen
/// taraf paleti yeniden çözmek zorunda kalmasın; yetkili değer yine anahtardır
/// (<paramref name="PaletteName"/>).
/// </summary>
public sealed record InstitutionBrandPaletteChanged(
    Guid InstitutionId,
    string PaletteName,
    string Primary,
    string Secondary);
