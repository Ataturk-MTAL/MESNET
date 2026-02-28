using Ardalis.SmartEnum;

namespace MESNET.Business.Core.Enums;

public sealed class BusinessSector : SmartEnum<BusinessSector>
{
    public static readonly BusinessSector JusticeAndSecurity = new(nameof(JusticeAndSecurity), 1, "Adalet ve Güvenlik");
    public static readonly BusinessSector WoodAndPaperProducts = new(nameof(WoodAndPaperProducts), 2, "Ağaç İşleri, Kağıt ve Kağıt Ürünleri");
    public static readonly BusinessSector InformationTechnology = new(nameof(InformationTechnology), 3, "Bilişim Teknolojileri");
    public static readonly BusinessSector GlassCementAndSoil = new(nameof(GlassCementAndSoil), 4, "Cam, Çimento ve Toprak");
    public static readonly BusinessSector Environment = new(nameof(Environment), 5, "Çevre");
    public static readonly BusinessSector Education = new(nameof(Education), 6, "Eğitim");
    public static readonly BusinessSector ElectricalAndElectronics = new(nameof(ElectricalAndElectronics), 7, "Elektrik ve Elektronik");
    public static readonly BusinessSector Energy = new(nameof(Energy), 8, "Enerji");
    public static readonly BusinessSector Finance = new(nameof(Finance), 9, "Finans");
    public static readonly BusinessSector Food = new(nameof(Food), 10, "Gıda");
    public static readonly BusinessSector Construction = new(nameof(Construction), 11, "İnşaat");
    public static readonly BusinessSector BusinessAndManagement = new(nameof(BusinessAndManagement), 12, "İş ve Yönetim");
    public static readonly BusinessSector ChemistryPetroleumRubberPlastics = new(nameof(ChemistryPetroleumRubberPlastics), 13, "Kimya, Petrol, Lastik ve Plastik");
    public static readonly BusinessSector CultureArtAndDesign = new(nameof(CultureArtAndDesign), 14, "Kültür, Sanat ve Tasarım");
    public static readonly BusinessSector Mining = new(nameof(Mining), 15, "Maden");
    public static readonly BusinessSector Machinery = new(nameof(Machinery), 16, "Makine");
    public static readonly BusinessSector MediaCommunicationAndPublishing = new(nameof(MediaCommunicationAndPublishing), 17, "Medya, İletişim ve Yayıncılık");
    public static readonly BusinessSector Metal = new(nameof(Metal), 18, "Metal");
    public static readonly BusinessSector Automotive = new(nameof(Automotive), 19, "Otomotiv");
    public static readonly BusinessSector HealthAndSocialServices = new(nameof(HealthAndSocialServices), 20, "Sağlık ve Sosyal Hizmetler");
    public static readonly BusinessSector SportsAndRecreation = new(nameof(SportsAndRecreation), 21, "Spor ve Rekreasyon");
    public static readonly BusinessSector AgricultureHuntingAndFishing = new(nameof(AgricultureHuntingAndFishing), 22, "Tarım, Avcılık ve Balıkçılık");
    public static readonly BusinessSector TextileApparelAndLeather = new(nameof(TextileApparelAndLeather), 23, "Tekstil, Hazır Giyim, Deri");
    public static readonly BusinessSector Commerce = new(nameof(Commerce), 24, "Ticaret (Satış ve Pazarlama)");
    public static readonly BusinessSector CommunityAndPersonalServices = new(nameof(CommunityAndPersonalServices), 25, "Toplumsal ve Kişisel Hizmetler");
    public static readonly BusinessSector TourismHospitalityAndFoodServices = new(nameof(TourismHospitalityAndFoodServices), 26, "Turizm, Konaklama, Yiyecek-İçecek Hizmetleri");
    public static readonly BusinessSector TransportLogisticsAndCommunication = new(nameof(TransportLogisticsAndCommunication), 27, "Ulaştırma, Lojistik ve Haberleşme");

    public string Slug { get; }

    private BusinessSector(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
