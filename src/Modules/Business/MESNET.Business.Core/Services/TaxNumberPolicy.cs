namespace MESNET.Business.Core.Services;

/// <summary>
/// İşletmenin vergi kimliğinin biçim kuralı (#150).
///
/// <para><b>Neden bu alan kritik:</b> işletme kataloğu okullar arası <b>paylaşımlıdır</b>
/// (ADR-0003 kısıt 2) — tüm okullar tüm işletmeleri görür. Aynı firmayı iki okul bağımsız
/// kaydederse ortaya kopya kayıtlar çıkar ve sonradan <b>birleştirilmeleri</b> gerekir:
/// sözleşmelerin, koordinasyon görünümlerinin ve devamsızlık kayıtlarının yeniden
/// yönlendirilmesi. Olay kaynaklı varlıklarda bu gerçekten acı verici bir iştir.</para>
///
/// <para>Vergi kimliği bu yüzden yalnız bir bilgi alanı değil, kataloğun <b>doğal
/// anahtarı</b>dır. Benzersizliği veritabanı kısıtıyla korunur; bu sınıf yalnız biçimi
/// doğrular.</para>
///
/// <para><b>İki uzunluk da geçerlidir:</b> tüzel kişilerde 10 haneli VKN, şahıs
/// işletmelerinde 11 haneli TC kimlik numarası kullanılır. Mesleki eğitimde staj veren
/// işletmelerin önemli bir bölümü şahıs işletmesidir; yalnız 10 haneyi kabul etmek onları
/// sisteme alamamak demektir.</para>
/// </summary>
public static class TaxNumberPolicy
{
    /// <summary>Tüzel kişi — vergi kimlik numarası.</summary>
    private const int VknLength = 10;

    /// <summary>Şahıs işletmesi — TC kimlik numarası.</summary>
    private const int TcknLength = 11;

    /// <summary>
    /// Karşılaştırma ve saklama için tekilleştirilmiş biçim: kenar boşlukları atılır.
    /// <b>Benzersizlik bu değer üzerinden kurulur</b>, dolayısıyla normalleştirme yazma
    /// yolunda uygulanmalıdır — aksi hâlde " 1234567890" ile "1234567890" iki ayrı kayıt olur
    /// ve kısıt hiçbir şey korumaz.
    /// </summary>
    public static string? Normalize(string? taxNumber) =>
        string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim();

    /// <summary>
    /// Biçim geçerli mi? Boş değer <b>geçersizdir</b>: alan zorunludur, çünkü boş bırakılabilseydi
    /// iki okul aynı firmayı boş anahtarla kaydeder ve kopya tam da engellenmek istenen yerde
    /// doğardı.
    /// </summary>
    public static bool IsValid(string? taxNumber)
    {
        if (Normalize(taxNumber) is not { } value)
            return false;

        if (value.Length is not (VknLength or TcknLength))
            return false;

        return value.All(char.IsAsciiDigit);
    }

    /// <summary>Doğrulama mesajı — tek yerde dursun ki uçlar ve testler ayrışmasın.</summary>
    public const string FormatMessage =
        "Vergi kimlik numarası 10 haneli (VKN) ya da 11 haneli (TC kimlik no) olmalı ve yalnız rakam içermelidir.";
}
