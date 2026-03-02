namespace MESNET.Coordination.Application.Queries;

/// <summary>
/// Bir öğretmenin tüm ders programı stream'lerini listele
/// (akademik yıl/dönem bazında ayrı ayrı)
/// </summary>
public sealed record GetScheduleStreams(Guid TeacherId);
