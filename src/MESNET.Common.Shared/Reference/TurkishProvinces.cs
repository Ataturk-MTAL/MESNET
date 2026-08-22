namespace MESNET.Common.Shared.Reference;

/// <summary>
/// Türkiye il kodu → il adı. MEB il kodu trafik tescil (plaka) koduyla aynıdır: <c>01</c>–<c>81</c>.
/// </summary>
/// <remarks>
/// Neden serbest metin yerine kod: kurumun ili kapsam kararının anahtarıdır (#147). Serbest metin
/// tutulduğunda <c>Kırşehir</c> / <c>Kirsehir</c> / <c>KIRŞEHİR</c> / sonda boşluklu hâli ayrı
/// değerlerdir ve ikinci il eklendiğinde hangisinin hangi il olduğu geriye dönük veri
/// arkeolojisine dönüşür.
///
/// Neden <c>Institution.Core</c> değil <c>Common.Shared</c>: il adı ulusal referans verisidir ve
/// ileride Business/Coordination tarafında da gerekebilir. Bu modüller
/// <c>MESNET.Institution.Core</c>'a referans VEREMEZ (modüller arası Core referansı yasak), o
/// yüzden liste ortak altyapıda durur.
///
/// Kod <c>string</c> tutulur, <c>int</c> değil: baştaki sıfır anlamlıdır (<c>01</c> Adana),
/// sayıya çevrilirse kaybolur.
/// </remarks>
public static class TurkishProvinces
{
    /// <summary>İl kodu uzunluğu — her zaman iki hane, sıfır dolgulu.</summary>
    public const int CodeLength = 2;

    private static readonly Dictionary<string, string> NamesByCode = new(StringComparer.Ordinal)
    {
        ["01"] = "Adana",
        ["02"] = "Adıyaman",
        ["03"] = "Afyonkarahisar",
        ["04"] = "Ağrı",
        ["05"] = "Amasya",
        ["06"] = "Ankara",
        ["07"] = "Antalya",
        ["08"] = "Artvin",
        ["09"] = "Aydın",
        ["10"] = "Balıkesir",
        ["11"] = "Bilecik",
        ["12"] = "Bingöl",
        ["13"] = "Bitlis",
        ["14"] = "Bolu",
        ["15"] = "Burdur",
        ["16"] = "Bursa",
        ["17"] = "Çanakkale",
        ["18"] = "Çankırı",
        ["19"] = "Çorum",
        ["20"] = "Denizli",
        ["21"] = "Diyarbakır",
        ["22"] = "Edirne",
        ["23"] = "Elazığ",
        ["24"] = "Erzincan",
        ["25"] = "Erzurum",
        ["26"] = "Eskişehir",
        ["27"] = "Gaziantep",
        ["28"] = "Giresun",
        ["29"] = "Gümüşhane",
        ["30"] = "Hakkâri",
        ["31"] = "Hatay",
        ["32"] = "Isparta",
        ["33"] = "Mersin",
        ["34"] = "İstanbul",
        ["35"] = "İzmir",
        ["36"] = "Kars",
        ["37"] = "Kastamonu",
        ["38"] = "Kayseri",
        ["39"] = "Kırklareli",
        ["40"] = "Kırşehir",
        ["41"] = "Kocaeli",
        ["42"] = "Konya",
        ["43"] = "Kütahya",
        ["44"] = "Malatya",
        ["45"] = "Manisa",
        ["46"] = "Kahramanmaraş",
        ["47"] = "Mardin",
        ["48"] = "Muğla",
        ["49"] = "Muş",
        ["50"] = "Nevşehir",
        ["51"] = "Niğde",
        ["52"] = "Ordu",
        ["53"] = "Rize",
        ["54"] = "Sakarya",
        ["55"] = "Samsun",
        ["56"] = "Siirt",
        ["57"] = "Sinop",
        ["58"] = "Sivas",
        ["59"] = "Tekirdağ",
        ["60"] = "Tokat",
        ["61"] = "Trabzon",
        ["62"] = "Tunceli",
        ["63"] = "Şanlıurfa",
        ["64"] = "Uşak",
        ["65"] = "Van",
        ["66"] = "Yozgat",
        ["67"] = "Zonguldak",
        ["68"] = "Aksaray",
        ["69"] = "Bayburt",
        ["70"] = "Karaman",
        ["71"] = "Kırıkkale",
        ["72"] = "Batman",
        ["73"] = "Şırnak",
        ["74"] = "Bartın",
        ["75"] = "Ardahan",
        ["76"] = "Iğdır",
        ["77"] = "Yalova",
        ["78"] = "Karabük",
        ["79"] = "Kilis",
        ["80"] = "Osmaniye",
        ["81"] = "Düzce"
    };

    /// <summary>Kod sırasına göre tüm iller.</summary>
    public static IReadOnlyList<KeyValuePair<string, string>> All { get; } =
        NamesByCode.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Kod bilinen bir il kodu mu. Büyük/küçük harf ya da kırpma yapılmaz — girdi tam olarak
    /// saklanacak biçimde olmalıdır, yoksa aynı il iki farklı değerle kaydedilir.
    /// </summary>
    public static bool IsValidCode(string? code) =>
        code is not null && NamesByCode.ContainsKey(code);

    /// <summary>İl adını döndürür; kod bilinmiyorsa <c>null</c>.</summary>
    public static string? GetName(string? code) =>
        code is not null && NamesByCode.TryGetValue(code, out var name) ? name : null;
}
