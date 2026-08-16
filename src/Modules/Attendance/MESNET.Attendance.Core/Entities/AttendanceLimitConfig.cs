namespace MESNET.Attendance.Core.Entities;

/// <summary>
/// Devamsızlık sınırları — <b>ulusal parametre</b> (#183, #147 deseni).
///
/// <para><b>Neden kurum ayarı değil:</b> sınır MEB Ortaöğretim Kurumları Yönetmeliği md. 36'dan
/// türer. Okul başına değişemez; değişirse mevzuat değişmiştir. Bu yüzden belge <b>kiracı
/// damgası taşımaz</b> (<c>DocumentTenancyMap</c> → <c>Shared</c>) ve yazma izni
/// <c>platform:parameter:manage</c>'dir — hiçbir okul rolünde yoktur.</para>
///
/// <para><b>Neden parametrik:</b> mevzuat değiştiğinde <b>kod değişmemeli</b>. Sabitler yalnız
/// kayıt hiç girilmemişken kullanılan başlangıç değerleridir.</para>
///
/// <para><b>Neden sürüm geçmişi yok</b> (asgari ücretin aksine): sınır, devamsızlık girildiği
/// <b>an</b> değerlendirilir. Asgari ücret geçmiş ayların maaşını hesaplamak için tarihli
/// tutulmak zorundaydı (#75); burada geriye dönük hesap yok.</para>
/// </summary>
public sealed class AttendanceLimitConfig
{
    /// <summary>Tekil kayıt — ulusal parametrenin tek satırı.</summary>
    public static readonly Guid SingletonId = Guid.Parse("a77e0d1c-0000-4000-8000-000000000183");

    public Guid Id { get; set; } = SingletonId;

    /// <summary>
    /// Örgün — <b>özürsüz</b> gün sınırı. Md. 36 (5) birinci ayak.
    /// </summary>
    public int FormalUnexcusedDayLimit { get; set; }

    /// <summary>
    /// Örgün — <b>toplam</b> gün sınırı (mazeretli, raporlu ve izinli dâhil). Md. 36 (5) ikinci
    /// ayak. İki ayak <b>ayrı ayrı</b> bağlayıcıdır: hangisi önce dolarsa fesih onunla tetiklenir.
    /// </summary>
    public int FormalTotalDayLimit { get; set; }

    /// <summary>
    /// MESEM — <b>toplam</b> gün sınırı. Yönetmelik işletme eğitimini yalnız 3308 izin hakkı
    /// toplamıyla karşılaştırır; devamsızlık türüne bakmaz. Bu yüzden MESEM'in <b>özürsüz
    /// karşılığı yoktur</b> ve simetri olsun diye uydurulmamıştır.
    /// </summary>
    public int MesemTotalDayLimit { get; set; }

    public Guid UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
