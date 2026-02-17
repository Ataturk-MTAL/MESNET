using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Core.Entities;

public sealed class BusinessEvaluation
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid EvaluatorId { get; set; }
    public DateTime EvaluationDate { get; set; }
    public List<EvaluationItem> Items { get; set; } = [];

    [JsonConverter(typeof(SmartEnumNameConverter<EvaluationResult, int>))]
    public EvaluationResult Result { get; set; } = EvaluationResult.Suitable;

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
