using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Common.Shared.Enums;

namespace MESNET.Institution.Core.ValueObjects;

public sealed record InstitutionBranch
{
    public required string FieldCode { get; init; }
    public required string FieldName { get; init; }

    [JsonConverter(typeof(SmartEnumNameConverter<EducationType, int>))]
    public required EducationType Type { get; init; }

    public List<string> ActiveSpecializations { get; init; } = [];
    public int AvailableCount { get; set; }
    public int AtWorkCount { get; set; }
    public int TotalCount { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Alan şefi sayısı (her alanda genelde 1)</summary>
    public int DepartmentHeadCount { get; set; }

    /// <summary>Atölye şefi sayısı (birden fazla olabilir)</summary>
    public int WorkshopHeadCount { get; set; }
}
