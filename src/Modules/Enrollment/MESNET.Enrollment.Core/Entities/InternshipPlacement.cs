using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Enrollment.Core.Enums;

namespace MESNET.Enrollment.Core.Entities;

public class InternshipPlacement
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
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

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}
