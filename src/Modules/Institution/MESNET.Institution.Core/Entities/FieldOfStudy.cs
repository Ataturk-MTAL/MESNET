using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ValueObjects;

namespace MESNET.Institution.Core.Entities;

public class FieldOfStudy
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    [JsonConverter(typeof(SmartEnumNameConverter<EducationType, int>))]
    public EducationType Type { get; set; } = EducationType.Formal;

    public List<Specialization> Specializations { get; set; } = [];
    public bool IsProtocolBased { get; set; }
    public bool IsActive { get; set; } = true;
}
