namespace MESNET.Security.Application.Commands;

/// <summary>
/// Rol modeli tutarlılık taraması (#129) — <b>yalnız tespit, düzeltme yok</b>.
///
/// <para>#129 öncesinde arayüz sistemde karşılığı olmayan rol adları (<c>deputy_director</c>,
/// <c>coordinator_teacher</c>, <c>master_trainer</c>) gönderebiliyordu ve sunucu bunu sessizce
/// kabul ediyordu. Bugünkü ortamda bozuk kayıt bulunabilir; bu sorgu onları listeler.</para>
///
/// <para><b>Otomatik düzeltme bilinçli olarak yoktur.</b> Kimin müdür yardımcısı kimin personel
/// olduğu okulun bilgisidir; kod tahmin edemez. Görünüşte apaçık eşlemeler bile
/// (<c>deputy_director</c> → <c>DeputyDirector</c>) yalnız <b>öneri</b> olarak döner.</para>
/// </summary>
public sealed record GetRoleIntegrityReport;
