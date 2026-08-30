using MESNET.Institution.Application.Security;

namespace MESNET.Institution.Application.Commands;

/// <summary>
/// Kurumun marka paletini küratörlü kümeden seçer (<c>institution:manage</c>).
///
/// <para><b>Gövde hex TAŞIMAZ</b>, yalnız palet anahtarını taşır
/// (<c>InstitutionBrandPalette.Name</c>: <c>Lacivert</c>, <c>Bordo</c>, ...). Serbest renk
/// kabul edilseydi arayüzün kontrast güvencesi istekle birlikte kırılabilirdi; anahtar
/// kapalı kümedir ve tanınmayan değer 422 ile reddedilir.</para>
///
/// <para>Kurum kimliği istekten geldiği için <see cref="IInstitutionScoped"/>: aktörün kurum
/// kapsamı hedefle eşleşmiyorsa handler hiç çalışmaz (ADR-0003 adım 6).</para>
/// </summary>
public sealed record SetInstitutionBrandPalette(
    Guid InstitutionId,
    string PaletteName) : IInstitutionScoped;
