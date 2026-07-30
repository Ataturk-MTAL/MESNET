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
    /// MEB ilçe kodu. İlçe kapsamı henüz karara bağlanmadı (#147), o yüzden zorunlu değil —
    /// alan şimdiden var ki ilçe gerektiğinde ayrım için ikinci bir geçiş gerekmesin.
    /// </summary>
    public string? DistrictCode { get; set; }

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
