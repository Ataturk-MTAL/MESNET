using MESNET.Attendance.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Devamsızlık sayacının kapsamı (#242): <b>öğrenci + akademik dönem</b>.
///
/// <para><b>Neden kritik:</b> bu sayaç doğrudan fesih tetikleyicisidir —
/// <c>AttendanceLimitExceeded</c> → <c>InternshipSaga</c> → otomatik fesih zinciri. Yanlış kapsam
/// ya öğrencinin stajını haksız yere sonlandırır ya da hiç sonlandırmaz.</para>
///
/// <para><b>Yaşanan iki hata:</b> görünümün kimliği yalnız <c>StudentId</c>'ydi ve
/// <c>AcademicPeriodId</c> alanı hiç yoktu; <c>BusinessId</c> ise yalnız ilk olayda yazılıyordu.
/// Sonuç: (1) aynı işletmede kalan öğrencide sayaç dönem başında <b>sıfırlanmıyor</b>, geçen
/// yılın günleri bu yılın eşiğine sayılıyordu; (2) işletme değişince handler'ın sorgusu görünümü
/// <b>bulamıyor</b>, <c>total</c> hep 1 kalıyor ve limit <b>bir daha hiç</b> tetiklenmiyordu.</para>
/// </summary>
public sealed class AttendanceCounterScopeTests
{
    private static readonly Guid Ogrenci = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BaskaOgrenci = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Donem = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid BaskaDonem = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // ─── Kimlik: aynı girdi hep aynı anahtarı verir ──────────────────────────────────

    [Fact]
    public void Ayni_ogrenci_ve_donem_ayni_anahtari_verir()
    {
        AttendanceCounterScope.KeyFor(Ogrenci, Donem)
            .ShouldBe(AttendanceCounterScope.KeyFor(Ogrenci, Donem));
    }

    /// <summary>
    /// <b>Asıl regresyon (1).</b> Dönem değişince anahtar değişir — sayaç yeni dönemde sıfırdan
    /// başlar. Anahtar yalnız öğrenciden türeseydi geçen yılın devamsızlığı bu yılın fesih
    /// eşiğine sayılırdı.
    /// </summary>
    [Fact]
    public void Donem_degisince_anahtar_degisir()
    {
        AttendanceCounterScope.KeyFor(Ogrenci, Donem)
            .ShouldNotBe(AttendanceCounterScope.KeyFor(Ogrenci, BaskaDonem));
    }

    [Fact]
    public void Ogrenci_degisince_anahtar_degisir()
    {
        AttendanceCounterScope.KeyFor(Ogrenci, Donem)
            .ShouldNotBe(AttendanceCounterScope.KeyFor(BaskaOgrenci, Donem));
    }

    /// <summary>
    /// İki kimlik <b>yer değiştirdiğinde</b> aynı anahtar üretilmemeli — basit XOR gibi simetrik
    /// bir birleştirme bu tuzağa düşer ve farklı kapsamlar tek satıra çökerdi.
    /// </summary>
    [Fact]
    public void Kimliklerin_sirasi_anlamlidir()
    {
        AttendanceCounterScope.KeyFor(Ogrenci, Donem)
            .ShouldNotBe(AttendanceCounterScope.KeyFor(Donem, Ogrenci));
    }

    /// <summary>
    /// <b>Asıl regresyon (2).</b> İşletme anahtara GİRMEZ. Girseydi öğrenci işletme
    /// değiştirdiğinde sayaç sıfırlanır ve yıl içinde iki işletmede toplam 38 mazeretsiz gün
    /// biriktiren öğrenci hiçbir eşiğe takılmazdı. Devamsızlık öğrencinin <b>eğitim yılına</b>
    /// ait bir kayıttır, işletmeye değil.
    /// </summary>
    [Fact]
    public void Anahtar_isletmeden_bagimsizdir()
    {
        var isletmeA = Guid.NewGuid();
        var isletmeB = Guid.NewGuid();

        AttendanceCounterScope.KeyFor(Ogrenci, Donem, isletmeA)
            .ShouldBe(AttendanceCounterScope.KeyFor(Ogrenci, Donem, isletmeB));
    }

    /// <summary>Anahtar okunabilir olmalı — operatör veritabanında satırı bulabilmeli.</summary>
    [Fact]
    public void Anahtar_iki_kimligi_de_okunabilir_tasir()
    {
        var key = AttendanceCounterScope.KeyFor(Ogrenci, Donem);

        key.ShouldContain(Ogrenci.ToString());
        key.ShouldContain(Donem.ToString());
    }

    // ─── Eşik kararı ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Eşik <b>dolduğunda</b> tetiklenir, aşıldığında değil: 20 limitte 20. gün fesih başlatır.
    /// Bugünkü davranış budur ve #242 onu <b>değiştirmez</b> — bu iş sayacın kapsamıyla ilgili.
    /// </summary>
    [Theory]
    [InlineData(19, 20, false)]
    [InlineData(20, 20, true)]
    [InlineData(21, 20, true)]
    public void Esik_doldugunda_tetiklenir(int total, int limit, bool expected)
    {
        AttendanceCounterScope.IsExceeded(total, limit).ShouldBe(expected);
    }
}
