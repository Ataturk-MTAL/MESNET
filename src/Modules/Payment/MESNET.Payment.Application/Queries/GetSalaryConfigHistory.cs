namespace MESNET.Payment.Application.Queries;

/// <summary>
/// Kurumun asgari ücret yürürlük geçmişi — geçmiş, yürürlükteki ve ileri tarihli dönemler.
/// </summary>
/// <remarks>
/// Kurum kapsamı istekte TAŞINMAZ; handler token'daki <c>institution_id</c> claim'inden okur
/// (CLAUDE.md — permission erişimi açar, kapsamı belirlemez).
/// </remarks>
public sealed record GetSalaryConfigHistory;
