using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Application.Handlers;

public static class GetBrandPalettesHandler
{
    /// <summary>
    /// Küratörlü palet kataloğu — koddan gelir, veritabanına gitmez. Uç noktanın kendi içinde
    /// liste kurmaması için handler'dan geçer (endpoint ince adaptör kuralı);
    /// <c>GetProvincesHandler</c> ile aynı desen.
    /// </summary>
    public static BrandPaletteListDto Handle(GetBrandPalettes _) =>
        new(InstitutionBrandPalette.List
            .OrderBy(p => p.Value)
            .Select(p => new BrandPaletteDto(
                p.Name,
                p.Slug,
                p.Primary,
                p.Secondary,
                p.Name == InstitutionBrandPalette.Default.Name))
            .ToList());
}
