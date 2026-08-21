namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// Kademeli devamsızlık bildirimi <b>gerekiyor</b> — md. 36 (4)'ün 5./15./25. gün eşiği doldu
/// (#247).
///
/// <para><b>Bu olay bir HÜKÜM değil, bir TEBLİGAT gereğidir.</b> Fesih zinciriyle karıştırılmamalı
/// — o ayrı bir eşikten (md. 36 (5)) <c>AttendanceLimitExceeded</c> ile doğar.</para>
///
/// <para>Teslimat tüketicilerin işidir: uygulama içi bildirim, e-posta ve (koordinatör isterse)
/// yazdırılabilir tebligat belgesi. Olay <b>alıcıları belirlemez</b>, alıcı çözümü teslimat
/// tarafındadır; ama 18 yaş kararı burada verilir çünkü doğum tarihi Attendance'ın yerel
/// görünümündedir.</para>
/// </summary>
/// <param name="Leg">
/// <c>Unexcused</c> ya da <c>Total</c>. İki ayak <b>ayrı</b> takip edilir ve ayrı bildirim üretir;
/// aynı gün ikisi birden dolabilir.
/// </param>
/// <param name="Step">Ulaşılan kademe — 5, 15 ya da 25.</param>
/// <param name="Days">Kademeyi dolduran gün sayısı.</param>
/// <param name="SkippedSteps">
/// Atlanan kademeler. Sayaç bir sıçramada birden çok kademeyi geçmiş olabilir; yalnız en yüksek
/// kademe bildirilir ve atlananlar burada kayda geçer — tebligatta "5. ve 15. gün bildirimleri
/// zamanında yapılamadı" bilgisi görünebilsin diye.
/// </param>
/// <param name="NotifyStudent">
/// Öğrencinin kendisine de bildirim yapılacak mı — md. 36 (4) bunu 18 yaşını doldurmuş öğrenciler
/// için istiyor. Doğum tarihi bilinmiyorsa <c>true</c>: gönderme yönü güvenlidir, veli ve işletme
/// ayakları zaten koşulsuzdur.
/// </param>
public sealed record AbsenceNotificationDue(
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string Leg,
    int Step,
    int Days,
    IReadOnlyList<int> SkippedSteps,
    bool NotifyStudent,
    DateTime OccurredAt);
