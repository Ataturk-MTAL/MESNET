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

    /// <summary>Örgün ortaöğretim — mazeretsiz gün sınırı.</summary>
    public int FormalUnexcusedDayLimit { get; set; }

    /// <summary>MESEM — mazeretsiz gün sınırı.</summary>
    public int MesemUnexcusedDayLimit { get; set; }

    public Guid UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
