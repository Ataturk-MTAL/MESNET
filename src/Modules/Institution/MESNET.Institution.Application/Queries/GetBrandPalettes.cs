namespace MESNET.Institution.Application.Queries;

/// <summary>
/// Küratörlü marka paleti kataloğu — kurum ayarlarındaki tema seçimi için.
///
/// <para>Kurum kapsamı taşımaz: katalog bütün okullar için aynıdır ve koddan gelir. Uç
/// noktanın olma sebebi, hex değerlerinin arayüzde <b>ikinci kez tanımlanmaması</b>dır;
/// iki kopya olsaydı biri güncellenir, diğeri ölçülmemiş renkle kalırdı.</para>
/// </summary>
public sealed record GetBrandPalettes;
