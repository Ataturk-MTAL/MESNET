using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Enrollment.Core.Enums;

namespace MESNET.Enrollment.Core.Entities;

public class InternshipPlacement
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }

    /// <summary>
    /// İşletme — <b>okulda stajda null</b> (#159). Staj yeri bulunamayan öğrencinin işvereni
    /// yoktur; ücret ve devlet katkısı doğmaz, dekont beklenmez.
    /// </summary>
    public Guid? BusinessId { get; set; }

    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    /// <summary>
    /// İşletmede stajda <b>koordinatör öğretmen</b>; okulda stajda <b>gözetmen</b> (alan ya da
    /// atölye şefi) — #159. İki anlam aynı alanda durur çünkü ücret tarafı ayrımı zaten yapıyor:
    /// koordinatörlük saati <c>BusinessCoordinationView</c>'dan doğar ve o satır
    /// (BusinessId, BranchCode, AcademicPeriodId) üçlüsünden üretildiği için işletmesiz
    /// yerleştirmede hiç oluşmaz. Gözetmenlik ataması ücret üretmez — kural sahibinin ifadesi:
    /// "atanabilir ancak kimse bunlardan ücret alamaz."
    /// </summary>
    public Guid? TeacherId { get; set; }

    /// <summary>Öğrenci adı — arama ve listeleme için denormalize</summary>
    public string StudentName { get; set; } = "";

    /// <summary>Öğrencinin branş kodu — filtre için denormalize</summary>
    public string BranchCode { get; set; } = "";

    [JsonConverter(typeof(SmartEnumNameConverter<PlacementStatus, int>))]
    public PlacementStatus Status { get; set; } = PlacementStatus.Matched;

    /// <summary>Marten LINQ sorguları için düz string kopyası</summary>
    public string StatusName { get; set; } = PlacementStatus.Matched.Name;

    [JsonConverter(typeof(SmartEnumNameConverter<ApplicationSource, int>))]
    public ApplicationSource Source { get; set; } = ApplicationSource.InstitutionAssignment;

    /// <summary>İşletmede mi okulda mı (#159).</summary>
    [JsonConverter(typeof(SmartEnumNameConverter<PlacementType, int>))]
    public PlacementType Type { get; set; } = PlacementType.Business;

    /// <summary>Marten LINQ sorguları için düz string kopyası (SmartEnum LINQ'te kullanılamaz).</summary>
    public string TypeName { get; set; } = PlacementType.Business.Name;

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}
