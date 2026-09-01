using MESNET.Common.Shared.Pagination;

namespace MESNET.Institution.Application.Queries;

/// <summary>
/// Yöneticisi olmayan okullar — bootstrap iş listesi (D2).
///
/// <para><b>Parametresizdir</b> (sayfalama dışında): kapsam istekten alınmaz, aktörün
/// claim'lerinden türer.</para>
/// </summary>
public sealed record GetUnmanagedInstitutions : PagedQuery;
