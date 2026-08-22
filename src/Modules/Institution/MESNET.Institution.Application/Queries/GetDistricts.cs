namespace MESNET.Institution.Application.Queries;

/// <summary>
/// Bir ilin ilçeleri — kurum formundaki ilçe seçimi için. Alfabetik sırada döner.
/// </summary>
public sealed record GetDistricts(string ProvinceCode);
