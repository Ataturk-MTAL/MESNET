using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Queries;

namespace MESNET.Institution.Application.Handlers;

public static class GetProvincesHandler
{
    /// <summary>
    /// Statik ulusal referans verisi — veritabanına gitmez. Uç noktanın kendi içinde liste
    /// döndürmemesi için handler'dan geçer (endpoint ince adaptör kuralı).
    /// </summary>
    public static ProvinceListDto Handle(GetProvinces _) =>
        new(TurkishProvinces.All.Select(p => new ProvinceDto(p.Key, p.Value)).ToList());
}
