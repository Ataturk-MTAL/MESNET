namespace MESNET.Common.Shared.Reference;

/// <summary>
/// İl kodu → o ilin ilçe adları, <b>Türkçe alfabetik sırada</b>.
/// </summary>
/// <remarks>
/// <para><b>Neden ilçede ad, ilde kod:</b> il kodu (plaka) resmî, tek ve kesin bilinen bir
/// koddur — <see cref="TurkishProvinces"/> tam listeyi taşır. İlçe için aynı güvenilirlikte
/// tek bir kod alanı yok (MEB, TÜİK ve mülki idare farklı numaralandırmalar kullanır).
/// Uydurulmuş bir ilçe kodu gerçek veri gibi görünür ve yanlış kodla açılmış kaydı geriye
/// dönük ayıklamak imkânsıza yakındır. Bu yüzden ilçe <b>kapalı listeden seçilen ad</b>
/// olarak tutulur.</para>
///
/// <para>Serbest metinden farkı: değer bu listede yoksa REDDEDİLİR. <c>Toroslar</c> /
/// <c>TOROSLAR</c> / <c>toroslar </c> gibi varyantlar oluşamaz — #147'nin serbest metne
/// itirazı burada da geçerlidir, çözüm kod değil kapalı liste.</para>
///
/// <para><b>Veri kaynağı ve doğruluğu:</b> liste ikincil bir kaynaktan alındı
/// (drdatastats.com). İçişleri Bakanlığı'nın resmî envanteri
/// (<c>e-icisleri.gov.tr/Anasayfa/MulkiIdariBolumleri.aspx</c>) veriyi yalnız etkileşimli
/// rapor olarak sunuyor, statik olarak indirilemedi. <b>Üretime çıkmadan resmî listeyle
/// karşılaştırılmalıdır.</b> Mülki idare bölünmeleri kanunla değişir (yeni ilçe kurulması,
/// ad değişikliği); değişiklikte bu dosya güncellenir.</para>
///
/// <para>Merkez ilçesi olan illerde ad <c>Merkez</c> olarak geçer. Büyükşehirlerde merkez
/// ilçe bölündüğü için böyle bir kayıt yoktur — eksiklik değildir.</para>
///
/// <para>Veri satır başına bir il tutulur: 973 ilçeyi ayrı ayrı dizi elemanı yazmak dosyayı
/// bin satırın üzerine çıkarır ve okunmaz hâle getirirdi. Ayrıştırma <see cref="Lazy{T}"/>
/// ile bir kez yapılır.</para>
/// </remarks>
public static class TurkishDistricts
{
    private static readonly Dictionary<string, string> Raw = new(StringComparer.Ordinal)
    {
        // 01 — Adana (15)
        ["01"] = "Aladağ,Ceyhan,Çukurova,Feke,İmamoğlu,Karaisalı,Karataş,Kozan,Pozantı,Saimbeyli,Sarıçam,Seyhan,Tufanbeyli,Yumurtalık,Yüreğir",
        // 02 — Adıyaman (9)
        ["02"] = "Besni,Çelikhan,Gerger,Gölbaşı,Kahta,Merkez,Samsat,Sincik,Tut",
        // 03 — Afyonkarahisar (18)
        ["03"] = "Başmakçı,Bayat,Bolvadin,Çay,Çobanlar,Dazkırı,Dinar,Emirdağ,Evciler,Hocalar,İhsaniye,İscehisar,Kızılören,Merkez,Sandıklı,Sinanpaşa,Sultandağı,Şuhut",
        // 04 — Ağrı (8)
        ["04"] = "Diyadin,Doğubayazıt,Eleşkirt,Hamur,Merkez,Patnos,Taşlıçay,Tutak",
        // 05 — Amasya (7)
        ["05"] = "Göynücek,Gümüşhacıköy,Hamamözü,Merkez,Merzifon,Suluova,Taşova",
        // 06 — Ankara (25)
        ["06"] = "Akyurt,Altındağ,Ayaş,Bala,Beypazarı,Çamlıdere,Çankaya,Çubuk,Elmadağ,Etimesgut,Evren,Gölbaşı,Güdül,Haymana,Kahramankazan,Kalecik,Keçiören,Kızılcahamam,Mamak,Nallıhan,Polatlı,Pursaklar,Sincan,Şereflikoçhisar,Yenimahalle",
        // 07 — Antalya (19)
        ["07"] = "Akseki,Aksu,Alanya,Demre,Döşemealtı,Elmalı,Finike,Gazipaşa,Gündoğmuş,İbradı,Kaş,Kemer,Kepez,Konyaaltı,Korkuteli,Kumluca,Manavgat,Muratpaşa,Serik",
        // 08 — Artvin (9)
        ["08"] = "Ardanuç,Arhavi,Borçka,Hopa,Kemalpaşa,Merkez,Murgul,Şavşat,Yusufeli",
        // 09 — Aydın (17)
        ["09"] = "Bozdoğan,Buharkent,Çine,Didim,Efeler,Germencik,İncirliova,Karacasu,Karpuzlu,Koçarlı,Köşk,Kuşadası,Kuyucak,Nazilli,Söke,Sultanhisar,Yenipazar",
        // 10 — Balıkesir (20)
        ["10"] = "Altıeylül,Ayvalık,Balya,Bandırma,Bigadiç,Burhaniye,Dursunbey,Edremit,Erdek,Gömeç,Gönen,Havran,İvrindi,Karesi,Kepsut,Manyas,Marmara,Savaştepe,Sındırgı,Susurluk",
        // 11 — Bilecik (8)
        ["11"] = "Bozüyük,Gölpazarı,İnhisar,Merkez,Osmaneli,Pazaryeri,Söğüt,Yenipazar",
        // 12 — Bingöl (8)
        ["12"] = "Adaklı,Genç,Karlıova,Kiğı,Merkez,Solhan,Yayladere,Yedisu",
        // 13 — Bitlis (7)
        ["13"] = "Adilcevaz,Ahlat,Güroymak,Hizan,Merkez,Mutki,Tatvan",
        // 14 — Bolu (9)
        ["14"] = "Dörtdivan,Gerede,Göynük,Kıbrıscık,Mengen,Merkez,Mudurnu,Seben,Yeniçağa",
        // 15 — Burdur (11)
        ["15"] = "Ağlasun,Altınyayla,Bucak,Çavdır,Çeltikçi,Gölhisar,Karamanlı,Kemer,Merkez,Tefenni,Yeşilova",
        // 16 — Bursa (17)
        ["16"] = "Büyükorhan,Gemlik,Gürsu,Harmancık,İnegöl,İznik,Karacabey,Keles,Kestel,Mudanya,Mustafakemalpaşa,Nilüfer,Orhaneli,Orhangazi,Osmangazi,Yenişehir,Yıldırım",
        // 17 — Çanakkale (12)
        ["17"] = "Ayvacık,Bayramiç,Biga,Bozcaada,Çan,Eceabat,Ezine,Gelibolu,Gökçeada,Lapseki,Merkez,Yenice",
        // 18 — Çankırı (12)
        ["18"] = "Atkaracalar,Bayramören,Çerkeş,Eldivan,Ilgaz,Kızılırmak,Korgun,Kurşunlu,Merkez,Orta,Şabanözü,Yapraklı",
        // 19 — Çorum (14)
        ["19"] = "Alaca,Bayat,Boğazkale,Dodurga,İskilip,Kargı,Laçin,Mecitözü,Merkez,Oğuzlar,Ortaköy,Osmancık,Sungurlu,Uğurludağ",
        // 20 — Denizli (19)
        ["20"] = "Acıpayam,Babadağ,Baklan,Bekilli,Beyağaç,Bozkurt,Buldan,Çal,Çameli,Çardak,Çivril,Güney,Honaz,Kale,Merkezefendi,Pamukkale,Sarayköy,Serinhisar,Tavas",
        // 21 — Diyarbakır (17)
        ["21"] = "Bağlar,Bismil,Çermik,Çınar,Çüngüş,Dicle,Eğil,Ergani,Hani,Hazro,Kayapınar,Kocaköy,Kulp,Lice,Silvan,Sur,Yenişehir",
        // 22 — Edirne (9)
        ["22"] = "Enez,Havsa,İpsala,Keşan,Lalapaşa,Meriç,Merkez,Süloğlu,Uzunköprü",
        // 23 — Elazığ (11)
        ["23"] = "Ağın,Alacakaya,Arıcak,Baskil,Karakoçan,Keban,Kovancılar,Maden,Merkez,Palu,Sivrice",
        // 24 — Erzincan (9)
        ["24"] = "Çayırlı,İliç,Kemah,Kemaliye,Merkez,Otlukbeli,Refahiye,Tercan,Üzümlü",
        // 25 — Erzurum (20)
        ["25"] = "Aşkale,Aziziye,Çat,Hınıs,Horasan,İspir,Karaçoban,Karayazı,Köprüköy,Narman,Oltu,Olur,Palandöken,Pasinler,Pazaryolu,Şenkaya,Tekman,Tortum,Uzundere,Yakutiye",
        // 26 — Eskişehir (14)
        ["26"] = "Alpu,Beylikova,Çifteler,Günyüzü,Han,İnönü,Mahmudiye,Mihalgazi,Mihalıççık,Odunpazarı,Sarıcakaya,Seyitgazi,Sivrihisar,Tepebaşı",
        // 27 — Gaziantep (9)
        ["27"] = "Araban,İslahiye,Karkamış,Nizip,Nurdağı,Oğuzeli,Şahinbey,Şehitkamil,Yavuzeli",
        // 28 — Giresun (16)
        ["28"] = "Alucra,Bulancak,Çamoluk,Çanakçı,Dereli,Doğankent,Espiye,Eynesil,Görele,Güce,Keşap,Merkez,Piraziz,Şebinkarahisar,Tirebolu,Yağlıdere",
        // 29 — Gümüşhane (6)
        ["29"] = "Kelkit,Köse,Kürtün,Merkez,Şiran,Torul",
        // 30 — Hakkâri (5)
        ["30"] = "Çukurca,Derecik,Merkez,Şemdinli,Yüksekova",
        // 31 — Hatay (15)
        ["31"] = "Altınözü,Antakya,Arsuz,Belen,Defne,Dörtyol,Erzin,Hassa,İskenderun,Kırıkhan,Kumlu,Payas,Reyhanlı,Samandağ,Yayladağı",
        // 32 — Isparta (13)
        ["32"] = "Aksu,Atabey,Eğirdir,Gelendost,Gönen,Keçiborlu,Merkez,Senirkent,Sütçüler,Şarkikaraağaç,Uluborlu,Yalvaç,Yenişarbademli",
        // 33 — Mersin (13)
        ["33"] = "Akdeniz,Anamur,Aydıncık,Bozyazı,Çamlıyayla,Erdemli,Gülnar,Mezitli,Mut,Silifke,Tarsus,Toroslar,Yenişehir",
        // 34 — İstanbul (39)
        ["34"] = "Adalar,Arnavutköy,Ataşehir,Avcılar,Bağcılar,Bahçelievler,Bakırköy,Başakşehir,Bayrampaşa,Beşiktaş,Beykoz,Beylikdüzü,Beyoğlu,Büyükçekmece,Çatalca,Çekmeköy,Esenler,Esenyurt,Eyüpsultan,Fatih,Gaziosmanpaşa,Güngören,Kadıköy,Kağıthane,Kartal,Küçükçekmece,Maltepe,Pendik,Sancaktepe,Sarıyer,Silivri,Sultanbeyli,Sultangazi,Şile,Şişli,Tuzla,Ümraniye,Üsküdar,Zeytinburnu",
        // 35 — İzmir (30)
        ["35"] = "Aliağa,Balçova,Bayındır,Bayraklı,Bergama,Beydağ,Bornova,Buca,Çeşme,Çiğli,Dikili,Foça,Gaziemir,Güzelbahçe,Karabağlar,Karaburun,Karşıyaka,Kemalpaşa,Kınık,Kiraz,Konak,Menderes,Menemen,Narlıdere,Ödemiş,Seferihisar,Selçuk,Tire,Torbalı,Urla",
        // 36 — Kars (8)
        ["36"] = "Akyaka,Arpaçay,Digor,Kağızman,Merkez,Sarıkamış,Selim,Susuz",
        // 37 — Kastamonu (20)
        ["37"] = "Abana,Ağlı,Araç,Azdavay,Bozkurt,Cide,Çatalzeytin,Daday,Devrekani,Doğanyurt,Hanönü,İhsangazi,İnebolu,Küre,Merkez,Pınarbaşı,Seydiler,Şenpazar,Taşköprü,Tosya",
        // 38 — Kayseri (16)
        ["38"] = "Akkışla,Bünyan,Develi,Felahiye,Hacılar,İncesu,Kocasinan,Melikgazi,Özvatan,Pınarbaşı,Sarıoğlan,Sarız,Talas,Tomarza,Yahyalı,Yeşilhisar",
        // 39 — Kırklareli (8)
        ["39"] = "Babaeski,Demirköy,Kofçaz,Lüleburgaz,Merkez,Pehlivanköy,Pınarhisar,Vize",
        // 40 — Kırşehir (7)
        ["40"] = "Akçakent,Akpınar,Boztepe,Çiçekdağı,Kaman,Merkez,Mucur",
        // 41 — Kocaeli (12)
        ["41"] = "Başiskele,Çayırova,Darıca,Derince,Dilovası,Gebze,Gölcük,İzmit,Kandıra,Karamürsel,Kartepe,Körfez",
        // 42 — Konya (31)
        ["42"] = "Ahırlı,Akören,Akşehir,Altınekin,Beyşehir,Bozkır,Cihanbeyli,Çeltik,Çumra,Derbent,Derebucak,Doğanhisar,Emirgazi,Ereğli,Güneysınır,Hadim,Halkapınar,Hüyük,Ilgın,Kadınhanı,Karapınar,Karatay,Kulu,Meram,Sarayönü,Selçuklu,Seydişehir,Taşkent,Tuzlukçu,Yalıhüyük,Yunak",
        // 43 — Kütahya (13)
        ["43"] = "Altıntaş,Aslanapa,Çavdarhisar,Domaniç,Dumlupınar,Emet,Gediz,Hisarcık,Merkez,Pazarlar,Simav,Şaphane,Tavşanlı",
        // 44 — Malatya (13)
        ["44"] = "Akçadağ,Arapgir,Arguvan,Battalgazi,Darende,Doğanşehir,Doğanyol,Hekimhan,Kale,Kuluncak,Pütürge,Yazıhan,Yeşilyurt",
        // 45 — Manisa (17)
        ["45"] = "Ahmetli,Akhisar,Alaşehir,Demirci,Gölmarmara,Gördes,Kırkağaç,Köprübaşı,Kula,Salihli,Sarıgöl,Saruhanlı,Selendi,Soma,Şehzadeler,Turgutlu,Yunusemre",
        // 46 — Kahramanmaraş (11)
        ["46"] = "Afşin,Andırın,Çağlayancerit,Dulkadiroğlu,Ekinözü,Elbistan,Göksun,Nurhak,Onikişubat,Pazarcık,Türkoğlu",
        // 47 — Mardin (10)
        ["47"] = "Artuklu,Dargeçit,Derik,Kızıltepe,Mazıdağı,Midyat,Nusaybin,Ömerli,Savur,Yeşilli",
        // 48 — Muğla (13)
        ["48"] = "Bodrum,Dalaman,Datça,Fethiye,Kavaklıdere,Köyceğiz,Marmaris,Menteşe,Milas,Ortaca,Seydikemer,Ula,Yatağan",
        // 49 — Muş (6)
        ["49"] = "Bulanık,Hasköy,Korkut,Malazgirt,Merkez,Varto",
        // 50 — Nevşehir (8)
        ["50"] = "Acıgöl,Avanos,Derinkuyu,Gülşehir,Hacıbektaş,Kozaklı,Merkez,Ürgüp",
        // 51 — Niğde (6)
        ["51"] = "Altunhisar,Bor,Çamardı,Çiftlik,Merkez,Ulukışla",
        // 52 — Ordu (19)
        ["52"] = "Akkuş,Altınordu,Aybastı,Çamaş,Çatalpınar,Çaybaşı,Fatsa,Gölköy,Gülyalı,Gürgentepe,İkizce,Kabadüz,Kabataş,Korgan,Kumru,Mesudiye,Perşembe,Ulubey,Ünye",
        // 53 — Rize (12)
        ["53"] = "Ardeşen,Çamlıhemşin,Çayeli,Derepazarı,Fındıklı,Güneysu,Hemşin,İkizdere,İyidere,Kalkandere,Merkez,Pazar",
        // 54 — Sakarya (16)
        ["54"] = "Adapazarı,Akyazı,Arifiye,Erenler,Ferizli,Geyve,Hendek,Karapürçek,Karasu,Kaynarca,Kocaali,Pamukova,Sapanca,Serdivan,Söğütlü,Taraklı",
        // 55 — Samsun (17)
        ["55"] = "19 Mayıs,Alaçam,Asarcık,Atakum,Ayvacık,Bafra,Canik,Çarşamba,Havza,İlkadım,Kavak,Ladik,Salıpazarı,Tekkeköy,Terme,Vezirköprü,Yakakent",
        // 56 — Siirt (7)
        ["56"] = "Baykan,Eruh,Kurtalan,Merkez,Pervari,Şirvan,Tillo",
        // 57 — Sinop (9)
        ["57"] = "Ayancık,Boyabat,Dikmen,Durağan,Erfelek,Gerze,Merkez,Saraydüzü,Türkeli",
        // 58 — Sivas (17)
        ["58"] = "Akıncılar,Altınyayla,Divriği,Doğanşar,Gemerek,Gölova,Gürün,Hafik,İmranlı,Kangal,Koyulhisar,Merkez,Suşehri,Şarkışla,Ulaş,Yıldızeli,Zara",
        // 59 — Tekirdağ (11)
        ["59"] = "Çerkezköy,Çorlu,Ergene,Hayrabolu,Kapaklı,Malkara,Marmaraereğlisi,Muratlı,Saray,Süleymanpaşa,Şarköy",
        // 60 — Tokat (12)
        ["60"] = "Almus,Artova,Başçiftlik,Erbaa,Merkez,Niksar,Pazar,Reşadiye,Sulusaray,Turhal,Yeşilyurt,Zile",
        // 61 — Trabzon (18)
        ["61"] = "Akçaabat,Araklı,Arsin,Beşikdüzü,Çarşıbaşı,Çaykara,Dernekpazarı,Düzköy,Hayrat,Köprübaşı,Maçka,Of,Ortahisar,Sürmene,Şalpazarı,Tonya,Vakfıkebir,Yomra",
        // 62 — Tunceli (8)
        ["62"] = "Çemişgezek,Hozat,Mazgirt,Merkez,Nazımiye,Ovacık,Pertek,Pülümür",
        // 63 — Şanlıurfa (13)
        ["63"] = "Akçakale,Birecik,Bozova,Ceylanpınar,Eyyübiye,Halfeti,Haliliye,Harran,Hilvan,Karaköprü,Siverek,Suruç,Viranşehir",
        // 64 — Uşak (6)
        ["64"] = "Banaz,Eşme,Karahallı,Merkez,Sivaslı,Ulubey",
        // 65 — Van (13)
        ["65"] = "Bahçesaray,Başkale,Çaldıran,Çatak,Edremit,Erciş,Gevaş,Gürpınar,İpekyolu,Muradiye,Özalp,Saray,Tuşba",
        // 66 — Yozgat (14)
        ["66"] = "Akdağmadeni,Aydıncık,Boğazlıyan,Çandır,Çayıralan,Çekerek,Kadışehri,Merkez,Saraykent,Sarıkaya,Sorgun,Şefaatli,Yenifakılı,Yerköy",
        // 67 — Zonguldak (8)
        ["67"] = "Alaplı,Çaycuma,Devrek,Ereğli,Gökçebey,Kilimli,Kozlu,Merkez",
        // 68 — Aksaray (8)
        ["68"] = "Ağaçören,Eskil,Gülağaç,Güzelyurt,Merkez,Ortaköy,Sarıyahşi,Sultanhanı",
        // 69 — Bayburt (3)
        ["69"] = "Aydıntepe,Demirözü,Merkez",
        // 70 — Karaman (6)
        ["70"] = "Ayrancı,Başyayla,Ermenek,Kazımkarabekir,Merkez,Sarıveliler",
        // 71 — Kırıkkale (9)
        ["71"] = "Bahşılı,Balışeyh,Çelebi,Delice,Karakeçili,Keskin,Merkez,Sulakyurt,Yahşihan",
        // 72 — Batman (6)
        ["72"] = "Beşiri,Gercüş,Hasankeyf,Kozluk,Merkez,Sason",
        // 73 — Şırnak (7)
        ["73"] = "Beytüşşebap,Cizre,Güçlükonak,İdil,Merkez,Silopi,Uludere",
        // 74 — Bartın (4)
        ["74"] = "Amasra,Kurucaşile,Merkez,Ulus",
        // 75 — Ardahan (6)
        ["75"] = "Çıldır,Damal,Göle,Hanak,Merkez,Posof",
        // 76 — Iğdır (4)
        ["76"] = "Aralık,Karakoyunlu,Merkez,Tuzluca",
        // 77 — Yalova (6)
        ["77"] = "Altınova,Armutlu,Çiftlikköy,Çınarcık,Merkez,Termal",
        // 78 — Karabük (6)
        ["78"] = "Eflani,Eskipazar,Merkez,Ovacık,Safranbolu,Yenice",
        // 79 — Kilis (4)
        ["79"] = "Elbeyli,Merkez,Musabeyli,Polateli",
        // 80 — Osmaniye (7)
        ["80"] = "Bahçe,Düziçi,Hasanbeyli,Kadirli,Merkez,Sumbas,Toprakkale",
        // 81 — Düzce (8)
        ["81"] = "Akçakoca,Cumayeri,Çilimli,Gölyaka,Gümüşova,Kaynaşlı,Merkez,Yığılca",
    };

    /// <summary>
    /// Sıralama <b>ayrıştırma anında</b> yapılır, ham veriye güvenilmez: kaynak listesi
    /// tr-TR sırasında değildi (ör. Yalova'da <c>Çiftlikköy, Çınarcık</c> geliyordu; doğrusu
    /// <c>Çınarcık, Çiftlikköy</c> — Türkçede <c>ı</c>, <c>i</c>'den ÖNCE gelir). Ham veriyi
    /// elle düzeltmek yerine burada sıralamak, sonradan eklenen ilin de sırasını garanti eder.
    /// </summary>
    private static readonly Lazy<Dictionary<string, string[]>> ByProvinceCode = new(() =>
    {
        var turkish = StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), ignoreCase: false);

        return Raw.ToDictionary(
            p => p.Key,
            p => p.Value.Split(',').OrderBy(d => d, turkish).ToArray(),
            StringComparer.Ordinal);
    });

    /// <summary>Bu il için ilçe listesi tanımlı mı. Değilse o ilde ilçe girilemez.</summary>
    public static bool IsKnown(string? provinceCode) =>
        provinceCode is not null && ByProvinceCode.Value.ContainsKey(provinceCode);

    /// <summary>İlin ilçeleri, alfabetik. Liste tanımlı değilse boş dizi.</summary>
    public static IReadOnlyList<string> For(string? provinceCode) =>
        provinceCode is not null && ByProvinceCode.Value.TryGetValue(provinceCode, out var districts)
            ? districts
            : [];

    /// <summary>
    /// İlçe adı, verilen ilin listesinde tam olarak var mı. Kırpma ya da büyük/küçük harf
    /// esnekliği YOKTUR — girdi saklanacak biçimde olmalı, yoksa aynı ilçe iki değerle kaydolur.
    /// </summary>
    public static bool IsValid(string? provinceCode, string? districtName) =>
        districtName is not null && For(provinceCode).Contains(districtName, StringComparer.Ordinal);
}
