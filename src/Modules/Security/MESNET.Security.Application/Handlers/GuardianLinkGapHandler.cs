using Marten;
using MESNET.Security.Application.Commands;
using MESNET.Security.Core.Entities;
using MESNET.Security.Core.ReadModels;

namespace MESNET.Security.Application.Handlers;

/// <summary>
/// Velisi bağlı OLMAYAN öğrenciler (#271) — eksiği <b>ölçülebilir</b> kılar.
///
/// <para><b>Neden gerekli:</b> veli–öğrenci bağı kurulmadan md. 36 (4) tebligatı (#247) veliye
/// <b>hiç ulaşmıyor</b> ve bu sessiz bir kayıp. <c>AbsenceNotificationEmailConsumer</c> alıcı
/// bulamayınca uyarı yazıyor ama log okunmazsa yükümlülük yerine getirilmemiş olur. Sayı
/// görünmeden kimse eksik olduğunu bilmez.</para>
///
/// <para><b>Neden bağ otomatik kurulamıyor:</b> ortak anahtar yok. <c>UserAccount</c>'ta TC alanı
/// bulunmuyor, <c>StudentRegistered</c> veli bilgisi taşımıyor ve ad eşleştirmesi güvenilmez.
/// Yeni veliler davet anında bağlanıyor (<c>CreateInvitation.StudentIds</c>); mevcutlar bu
/// listeden görülüp elle bağlanır (<c>POST /api/security/users/{id}/students</c>).</para>
///
/// <para><b>Kiracı kapsamı:</b> <c>GuardianLinkView</c> kiracı damgalıdır, yani liste isteği
/// yapan okulun öğrencileriyle sınırlıdır. <c>UserAccount</c> kimlik katmanındadır ve damga
/// taşımaz — burada yalnız <b>üyelik</b> sorulduğu için (bu öğrenci herhangi bir hesaba bağlı
/// mı) okullar arası bir sızıntı doğurmaz: dönen veri hep kendi öğrencilerimizdir.</para>
/// </summary>
public static class GuardianLinkGapHandler
{
    public static async Task<GuardianLinkGapResult> Handle(
        GetStudentsWithoutGuardian query, IQuerySession session, CancellationToken ct)
    {
        var students = await session.Query<GuardianLinkView>().ToListAsync(ct);

        // Mezar taşı hesaplar bağ saymaz (#210): silinmiş bir veli hesabı bağlıymış gibi
        // görünürse eksik gizlenir.
        var accounts = await session.Query<UserAccount>()
            .Where(u => u.DeletedAt == null)
            .ToListAsync(ct);

        var linked = accounts.SelectMany(u => u.LinkedStudentIds).ToHashSet();

        var missing = students
            .Where(s => !linked.Contains(s.Id))
            .OrderBy(s => s.FullName, StringComparer.CurrentCulture)
            .Select(s => new StudentWithoutGuardianDto(s.Id, s.FullName, s.StudentNumber, s.BranchCode))
            .ToList();

        return new GuardianLinkGapResult(students.Count, missing.Count, missing);
    }
}
