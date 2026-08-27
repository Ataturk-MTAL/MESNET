using FluentValidation;
using MESNET.Common.Shared.Reference;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Application.Validators;

public class CreateInstitutionValidator : AbstractValidator<CreateInstitution>
{
    public CreateInstitutionValidator()
    {
        // Kurum kodu OKUL için zorunludur. İl/ilçe müdürlüklerinin kendi MEB kodları vardır
        // ama sistem onları bilmiyor ve uydurulmuş bir kod gerçek veri gibi görünürdü; sıfır
        // "girilmedi" demektir (aynı gerekçe InstitutionHierarchyPlanner içinde de yazılı).
        RuleFor(x => x.InstitutionCode)
            .GreaterThan(0)
            .When(x => IsSchool(x.NodeType))
            .WithMessage("Kurum kodu sıfırdan büyük olmalıdır.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Kurum adı belirtilmelidir.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");

        // İl kodu kayıt anında ZORUNLU (#147). Sonradan doldurulabilir bırakılırsa ikinci il
        // eklendiğinde ayrım yapılamayan kayıtlar birikir ve elle backfill gerekir.
        RuleFor(x => x.ProvinceCode).NotEmpty().WithMessage("İl belirtilmelidir.");
        RuleFor(x => x.ProvinceCode)
            .Must(TurkishProvinces.IsValidCode)
            .When(x => !string.IsNullOrWhiteSpace(x.ProvinceCode))
            .WithMessage("Geçerli bir MEB il kodu giriniz (01–81).");

        // İlçe ile il birlikte doğrulanır: ilçe adı TEK BAŞINA anlamlı değildir, hangi ilin
        // ilçesi olduğu bilinmeden doğrulanamaz. Aynı ad birden çok ilde geçebilir.
        RuleFor(x => x.DistrictName)
            .Must((command, district) => TurkishDistricts.IsValid(command.ProvinceCode, district))
            .When(x => !string.IsNullOrWhiteSpace(x.DistrictName))
            .WithMessage(x => DistrictMessage(x.ProvinceCode));

        // Küme YAZMA sınırında kapalıdır. InstitutionNodeType.Resolve tanınmayan değeri
        // sessizce School yapar — o hoşgörü OKUMA tarafı içindir (eski kayıtlar). Burada
        // sessiz kalsaydı kullanıcı il müdürlüğü açtığını sanırken bir okul doğardı.
        RuleFor(x => x.NodeType)
            .Must(name => InstitutionNodeType.TryFromName(name, ignoreCase: true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.NodeType))
            .WithMessage("Geçerli bir kurum türü seçiniz (Province / District / School).");

        // İl müdürlüğü köktür; üstü olamaz. İzin verilseydi ağaç modellenen üç seviyeyi aşar
        // ve "il yetkilisinin üstündeki il yetkilisi" gibi anlamsız bir kapsam doğardı.
        RuleFor(x => x.ParentId)
            .Empty()
            .When(x => string.Equals(x.NodeType, InstitutionNodeType.Province.Name,
                StringComparison.OrdinalIgnoreCase))
            .WithMessage("İl müdürlüğü kök düğümdür, üst kurumu olamaz.");
    }

    /// <summary>Tip verilmediğinde varsayılan okuldur — bugüne kadarki bütün çağrılar okul açıyordu.</summary>
    private static bool IsSchool(string? nodeType) =>
        string.IsNullOrWhiteSpace(nodeType)
        || string.Equals(nodeType, InstitutionNodeType.School.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// İlçe listesi bulunmayan il ile ilçe listesinde olmayan ad ayrı hatalardır: ilki eksik
    /// referans verisi (çözümü listeyi doldurmak), ikincisi hatalı giriş. Aynı mesajı vermek
    /// kullanıcıyı olmayan bir yazım hatasını aramaya iterdi.
    /// </summary>
    internal static string DistrictMessage(string? provinceCode) =>
        TurkishDistricts.IsKnown(provinceCode)
            ? "Seçilen ile ait geçerli bir ilçe seçiniz."
            : "Bu il için ilçe listesi tanımlı değil, ilçe girilemez.";
}
