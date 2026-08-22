namespace MESNET.Attendance.Core.Entities;

/// <summary>
/// Kademeli devamsızlık bildiriminin <b>gönderim defteri</b> — öğrenci + akademik dönem başına
/// tek satır (#247).
///
/// <para><b>Neden ayrı bir belge:</b> "bu kademeyi daha önce bildirdim mi" bilgisi hiçbir yerden
/// <b>türetilemez</b>. Devamsızlık sayacı agregadan canlı okunuyor (#249) ve dönem içinde
/// sıfırlanmıyor; yani eşik dolduktan sonra gelen her yeni kayıt/onay aynı kademeyi yeniden
/// tetiklerdi. Defter olmadan bildirim yağmuru <b>kaçınılmazdır</b>.</para>
///
/// <para><b>Neden <c>AttendanceView</c> kullanılmadı:</b> o belge async projeksiyon-sahiplidir ve
/// dağıtım yordamı yeniden inşayı zorunlu kılıyor (TRUNCATE + progression sil) — gönderim
/// damgaları olay akışından yeniden üretilemez, silinir ve <b>tüm okula 5/15/25 tebligatları
/// yeniden gider</b>. Üstelik o görünümün sayıları da yanlış: düzeltme/silme/rapor onayı
/// uygulanmıyor, monoton artıyor.</para>
///
/// <para><b>Ayak başına TEK MONOTON int:</b> küme değil. Toplu girişte sıçrama (0 → 17) tek
/// kararla çözülür ve "hangi kademeler atlandı" sorusu açıkça yanıtlanır
/// (<c>AbsenceNotificationPolicy.Evaluate</c>).</para>
///
/// <para><b>Kademe GERİ ALINMAZ.</b> Sayaç düzeltme/silme/rapor onayıyla düşebilir; yapılmış bir
/// tebligat yapılmamış sayılamaz. Sayaç düşüp yeniden aynı kademeye çıkarsa ikinci bildirim
/// gitmez.</para>
/// </summary>
public class AbsenceNotificationLog
{
    /// <summary>
    /// <c>{studentId}:{academicPeriodId}</c> — <c>AttendanceCounterScope.KeyFor</c> ile aynı
    /// okunabilir bileşik anahtar. Operatörün satırı gözle bulabilmesi gerekiyor.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>Kiracı anahtarı (#147) — belge <c>DocumentTenancyMap</c>'te <c>Tenant</c>.</summary>
    public Guid InstitutionId { get; set; }

    public Guid StudentId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    /// <summary>Mazeretsiz ayağında en son bildirilen kademe — 0 (hiç), 5, 15 ya da 25.</summary>
    public int LastNotifiedUnexcusedStep { get; set; }

    /// <summary>Toplam ayağında en son bildirilen kademe — 0 (hiç), 5, 15 ya da 25.</summary>
    public int LastNotifiedTotalStep { get; set; }

    public DateTime? LastUnexcusedNotifiedAt { get; set; }
    public DateTime? LastTotalNotifiedAt { get; set; }

    /// <summary>Bu ayak için en son bildirilen kademeyi okur.</summary>
    public int StepFor(string leg) => leg == Policies.AbsenceNotificationLeg.Unexcused
        ? LastNotifiedUnexcusedStep
        : LastNotifiedTotalStep;

    /// <summary>Bu ayak için kademeyi ilerletir. Kademe geri ALINMAZ — bkz. sınıf özeti.</summary>
    public void Advance(string leg, int step, DateTime at)
    {
        if (leg == Policies.AbsenceNotificationLeg.Unexcused)
        {
            if (step <= LastNotifiedUnexcusedStep) return;
            LastNotifiedUnexcusedStep = step;
            LastUnexcusedNotifiedAt = at;
            return;
        }

        if (step <= LastNotifiedTotalStep) return;
        LastNotifiedTotalStep = step;
        LastTotalNotifiedAt = at;
    }
}
