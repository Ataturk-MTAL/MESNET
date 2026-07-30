using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Queries;

namespace MESNET.Institution.Application.Handlers;

public static class GetDistrictsHandler
{
    /// <summary>
    /// Statik referans verisi — veritabanına gitmez. Listesi tanımlı olmayan il için BOŞ liste
    /// döner; bu hata değildir, "bu il için ilçe girilemez" demektir.
    /// </summary>
    public static DistrictListDto Handle(GetDistricts query) =>
        new([.. TurkishDistricts.For(query.ProvinceCode)]);
}
