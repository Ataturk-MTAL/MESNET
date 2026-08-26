using MESNET.Common.Shared;
using MESNET.Institution.Core.ValueObjects;

namespace MESNET.Institution.Core.Entities;

public class Institution
{
    public Guid Id { get; set; }
    public int InstitutionCode { get; set; }
    public required string FullName { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// Kurumun ili — MEB il kodu (<c>01</c>–<c>81</c>, plaka koduyla aynı). Kapsam kararının
    /// anahtarıdır (#147): dağıtım bugün tek il olsa da ayrım veride durur, yapılandırmada
    /// durmaz. Buraya serbest metin il adı YAZILMAZ — <c>TurkishProvinces</c> ile doğrulanır.
    /// </summary>
    /// <remarks>
    /// Nullable, çünkü mevcut kayıtlar bu alan olmadan saklandı. <c>required</c> yapılırsa
    /// System.Text.Json eksik alan yüzünden her eski kurumun okunmasını
    /// <c>JsonException</c> ile keser. Varlık zorunluluğu yazma sınırında (validator) uygulanır.
    /// Eski serbest metin <c>City</c>/<c>District</c> alanları kaldırıldı: hiçbir yerde
    /// yazılmıyor, okunmuyor ve DTO'ya çıkmıyordu; kod alanının yanında durmaları hangisinin
    /// yetkili olduğu sorusunu üretirdi.
    /// </remarks>
    public string? ProvinceCode { get; set; }

    /// <summary>
    /// İlçe adı — <c>TurkishDistricts</c> kapalı listesinden, ile bağlı. Serbest metin DEĞİL:
    /// listede olmayan değer reddedilir, böylece aynı ilçe iki farklı yazımla kaydolamaz.
    /// İlçede kod yerine ad tutulur çünkü ilde plaka kodu kadar güvenilir bir ilçe kodu
    /// kaynağı yok; uydurulmuş kod gerçek veri gibi görünürdü (#147).
    /// </summary>
    public string? DistrictName { get; set; }

    /// <summary>
    /// Kurumun seçtiği marka paletinin <b>anahtarı</b> —
    /// <c>InstitutionBrandPalette.Name</c> değeri (<c>Lacivert</c>, <c>Bordo</c>, ...).
    /// </summary>
    /// <remarks>
    /// <para><b>Neden ham hex değil anahtar:</b> arayüzün bütün kontrast güvencesi primary'ye
    /// bağlıdır. Buraya serbest hex saklansaydı, veriye düşen tek bozuk değer üst bardaki,
    /// birincil butondaki ve rozetlerdeki <b>beyaz metni okunmaz</b> yapardı ve bunu ne
    /// derleyici ne de bir test görebilirdi. Anahtar saklanınca palet kodda yaşar
    /// (<see cref="MESNET.Institution.Core.Enums.InstitutionBrandPalette"/>) ve kontrast
    /// kapıları testle kilitlenir.</para>
    ///
    /// <para><b>Null ne demek:</b> kurum henüz seçim yapmadı → varsayılan palet
    /// (<c>Lacivert</c> / Mührü Lacivert) geçerlidir. Tanınmayan bir değer de aynı yere düşer;
    /// çeviri tek noktada, <c>InstitutionBrandPalette.Resolve</c> içindedir.</para>
    ///
    /// <para>Nullable, <c>required</c> DEĞİL: mevcut kayıtlar bu alan olmadan saklandı;
    /// <c>required</c> yapılırsa System.Text.Json eksik alan yüzünden her eski kurumun
    /// okunmasını <c>JsonException</c> ile keser (aynı tuzak <see cref="ProvinceCode"/>
    /// yorumunda anlatıldı).</para>
    /// </remarks>
    public string? BrandPaletteName { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? WebUrl { get; set; }
    public Location? Location { get; set; }
    public ScheduleConfiguration? ScheduleConfig { get; set; }
    public List<InstitutionBranch> Branches { get; set; } = [];
    public List<StaffMember> Staff { get; set; } = [];
}

public sealed class ScheduleConfiguration
{
    /// <summary>
    /// Günlük ders sayısı (örn: 8 ders/gün)
    /// </summary>
    public int DailyPeriodCount { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Son değişikliği yapan kullanıcının kimliği — token'dan gelir, istekten ALINMAZ (#137).
    /// Ad sorgu tarafında <c>UserNameView</c>'dan çözülür. Eski <c>updatedBy</c> JSON
    /// anahtarı (serbest metin ad) bu adla artık okunmaz.
    /// </summary>
    public Guid UpdatedById { get; set; }
}
