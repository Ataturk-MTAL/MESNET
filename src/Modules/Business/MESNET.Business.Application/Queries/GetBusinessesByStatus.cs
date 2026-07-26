using MESNET.Common.Shared.Pagination;

namespace MESNET.Business.Application.Queries;

/// <summary>
/// <paramref name="BranchCode"/> verilirse yalnız o alandan öğrenci almaya AKTİF yetkili
/// işletmeler döner — yerleştirme ekranının kaynağı (#119).
/// </summary>
public sealed record GetBusinessesByStatus(
    string? Status,
    string? Sector = null,
    string? BranchCode = null) : PagedQuery;
