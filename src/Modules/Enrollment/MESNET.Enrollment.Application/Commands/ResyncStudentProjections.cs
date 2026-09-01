namespace MESNET.Enrollment.Application.Commands;

/// <summary>
/// Tüm öğrenciler için StudentRegistered event'ini yeniden yayınlar — diğer modüllerin
/// denormalize öğrenci read-model'lerini (Attendance/Contract StudentNameView, Reporting/Payment
/// view'ları) tazeler. Read-model ŞEMASI değiştiğinde (yeni alan, örn. StudentNameView.BranchCode)
/// idempotent seeder mevcut öğrencileri yeniden yaratmadığından eski document'lar bayat kalır;
/// bu komut event'i replay ederek backfill yapar. Tüm consumer'lar idempotent upsert (session.Store).
/// </summary>
public sealed record ResyncStudentProjections;

/// <param name="StudentCount">Onarım olayı yayınlanan öğrenci sayısı.</param>
/// <param name="CountScopesSynced">
/// Şube sayacı yeniden hesaplanan (kurum, akademik dönem) çifti sayısı (#290). Sıfır öğrenci
/// sıfır kapsam demektir; ikisinin ayrı raporlanması, sayacın gerçekten tazelenip
/// tazelenmediğini yanıttan görünür kılar.
/// </param>
public sealed record ResyncStudentProjectionsResult(int StudentCount, int CountScopesSynced);
