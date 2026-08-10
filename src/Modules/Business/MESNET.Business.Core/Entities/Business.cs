using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.Policies;
using MESNET.Business.Core.ValueObjects;
using MESNET.Common.Shared;

namespace MESNET.Business.Core.Entities;

public class Business
{
    public Guid Id { get; set; }
    /// <summary>
    /// İşletmeyi <b>kaydeden</b> okul — <b>provenance</b>, kapsam DEĞİL (ADR-0003 adım 4).
    ///
    /// <para><b>Bu alan bir kiracı anahtarı değildir ve filtre olarak kullanılmamalıdır.</b>
    /// İşletme kataloğu <b>paylaşımlıdır</b>: bir işletme birden çok okuldan öğrenci alır ve
    /// tüm okullar tüm işletmeleri listeler (<c>DocumentTenancyMap</c> → <c>Shared</c>).
    /// Kaydı ilk kimin girdiği, o işletmenin kime ait olduğunu söylemez.</para>
    ///
    /// <para><b>Ad neden değişti:</b> alan <c>InstitutionId</c> adındayken "bu işletme şu
    /// okula ait" diye okunuyordu. Conjoined kiracılık açıldığında (adım 5) bu okuma,
    /// paylaşımlı kataloğu kiracıya bölmeye — yani bir okulun diğerinin işletmesini hiç
    /// görememesine — yol açardı. Adın yalan söylememesi, bu kararın tek yapısal koruması.</para>
    ///
    /// <para><b>Geçmiş:</b> eskiden <c>TenantId</c> adındaydı; taşıdığı değer her zaman kurum
    /// kimliğiydi ama <c>Institution.TenantId</c> bambaşka bir literal tutuyordu (#147).</para>
    ///
    /// <para>Kayıt anında token'daki kurumdan doldurulur, istekten ALINMAZ.</para>
    /// </summary>
    public Guid RegisteredByInstitutionId { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    private BusinessStatus _status = BusinessStatus.PendingApproval;

    [JsonConverter(typeof(SmartEnumNameConverter<BusinessStatus, int>))]
    public BusinessStatus Status
    {
        get => _status;
        set { _status = value; StatusName = value.Name; }
    }

    // SmartEnum LINQ tuzağı: Status JSON'a düz string serialize edilir; sorgular için düz string kopya.
    public string StatusName { get; private set; } = BusinessStatus.PendingApproval.Name;

    [JsonConverter(typeof(SmartEnumNameConverter<RegistrationSource, int>))]
    public RegistrationSource Source { get; set; } = RegistrationSource.InstitutionRegistered;

    public int PersonnelCount { get; set; }

    /// <summary>
    /// İşletme bir kamu kurum/kuruluşu mu (#157).
    ///
    /// <para>3308 Geçici Madde 12: <b>"Kamu kurum ve kuruluşlarına Devlet katkısı ödenmez."</b>
    /// Bu alan o hükmün tek girdisidir; <c>false</c> kalırsa kamu kurumuna hak edilmeyen
    /// devlet katkısı hesaplanır.</para>
    ///
    /// <para>Varsayılan <c>false</c> (özel işletme) bilinçlidir: bu alandan önce yazılmış
    /// kayıtlar bugünkü davranışı korur. Mevcut kamu kurumları <b>elle işaretlenmelidir</b> —
    /// sistem bunu türetemez.</para>
    /// </summary>
    public bool IsPublicInstitution { get; set; }
    public Location? Location { get; set; }
    public BusinessCapacity Capacity { get; set; } = new();
    public List<BusinessRepresentative> Representatives { get; set; } = [];
    public string? TaxNumber { get; set; }
    public string? SgkNumber { get; set; }
    public string? ActivityField { get; set; }
    public List<string> Sectors { get; set; } = [];

    private List<BranchAuthorization> _authorizedBranches = [];

    /// <summary>
    /// İşletmenin öğrenci alabileceği alanlar — idarenin belge incelemesi sonucu verdiği yetkiler (#119).
    /// Boş liste "hepsine açık" değil, KAPALI anlamına gelir.
    /// </summary>
    public List<BranchAuthorization> AuthorizedBranches
    {
        get => _authorizedBranches;
        set
        {
            _authorizedBranches = value ?? [];
            ActiveBranchCodes = BranchAuthorizationPolicy.ActiveCodes(_authorizedBranches);
        }
    }

    /// <summary>
    /// SmartEnum/nested-path LINQ tuzağı ile aynı gerekçe: aktif alan kodlarının düz string kopyası.
    /// Marten sorguları (ör. yerleştirme ekranının alan filtresi) bu alan üzerinden çalışır.
    /// Setter <see cref="AuthorizedBranches"/> tarafından otomatik senkron tutulur.
    /// </summary>
    public List<string> ActiveBranchCodes { get; private set; } = [];

    public MasterInstructorInfo? MasterInstructor { get; set; }
    public List<BusinessDocument> Documents { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool HasAssignedTeacher { get; set; }
}
