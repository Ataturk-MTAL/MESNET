using Marten;
using MESNET.Contract.Shared.Events;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Core.Policies;
using MESNET.Internship.Shared.Events;

namespace MESNET.Internship.Application.Consumers;

// Sınıf adı Handler veya Consumer ile BİTMELİ — Wolverine tip keşfi konvansiyonu bu.
// Eski adı `InternshipSummaryUpdater` idi; hiç keşfedilmiyordu, dolayısıyla buradaki Handle
// metotları hiç çalışmıyordu. Tüketicisi olmayan olay dead letter üretmediği için hata sessizdi.
public static class InternshipSummaryConsumer
{
    public static void Handle(InternshipStarted e, IDocumentSession session)
    {
        var summary = new InternshipSummary
        {
            Id = e.InternshipId,
            PlacementId = e.PlacementId,
            StudentId = e.StudentId,
            StudentName = e.StudentName,
            BusinessId = e.BusinessId,
            BusinessName = e.BusinessName,
            InstitutionId = e.InstitutionId,
            AcademicPeriodId = e.AcademicPeriodId,
            Phase = InternshipPhase.AwaitingContract,
            StartedAt = e.StartedAt,
            LastUpdated = DateTime.UtcNow
        };
        session.Store(summary);
    }

    /// <summary>
    /// Sözleşme aktifleştiğinde özeti bağlar.
    ///
    /// <para><b>Eşleşme SagaRelayConsumer ile AYNI kuraldan geçer (#295).</b> Eski hâli yalnız
    /// <c>StudentId</c>'ye bakıyordu: ne işletme, ne faz süzgeci, ne sıralama. <c>InternshipSummary</c>
    /// yerleştirme başına doğar, yani fesih + yeniden yerleştirme yaşamış öğrencinin BİRDEN ÇOK
    /// satırı olur ve sorgu bunlardan Postgres'in döndürdüğü ilkini alırdı — o sıra kararlı
    /// değildir (güncellenen satır heap'te yer değiştirir).</para>
    ///
    /// <para><b>Neden aynı politika, kendi süzgecim değil:</b> özet ile saga <b>aynı satırı</b>
    /// seçmek zorundadır. Ayrı kurallar yazılsaydı özet, saga'nın ilerlettiğinden BAŞKA bir
    /// stajı anlatabilirdi ve iki kayıt sessizce ayrışırdı.</para>
    ///
    /// <para>Faz süzgeci LINQ'te DEĞİL bellekte uygulanır: <c>InternshipPhase</c> bir SmartEnum
    /// ve Marten LINQ'inde karşılaştırılamaz (bkz. CLAUDE.md).</para>
    /// </summary>
    public static async Task Handle(ContractActivated e, IDocumentSession session)
    {
        var candidates = await session.Query<InternshipSummary>()
            .Where(s => s.StudentId == e.StudentId)
            .ToListAsync();

        var summary = candidates.FirstOrDefault(s => SagaCorrelationPolicy.MatchesContract(
            s.StudentId, s.BusinessId, s.Phase, e.StudentId, e.BusinessId));
        if (summary is null) return;

        summary.ContractId = e.ContractId;
        summary.Phase = InternshipPhase.Active;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(InternshipCompleted e, IDocumentSession session)
    {
        var summary = await session.LoadAsync<InternshipSummary>(e.InternshipId);
        if (summary is null) return;

        summary.Phase = InternshipPhase.Completed;
        summary.CompletedAt = e.CompletedAt;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(InternshipTerminationApprovalChainStarted e, IDocumentSession session)
    {
        var summary = await session.LoadAsync<InternshipSummary>(e.InternshipId);
        if (summary is null) return;

        summary.Phase = InternshipPhase.TerminationInProgress;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    /// <summary>
    /// Yeniden yerleştirme talebinde eski stajın özetini kapatır.
    ///
    /// <para><b>Aynı kural, aynı gerekçe (#295).</b> Eski hâli işletmeye bakıyordu ama FAZA
    /// bakmıyordu: aynı işletmede daha önce feshedilmiş bir staj varsa, kapatılacak satır
    /// ikisinden hangisinin döndüğüne bağlıydı. Politika kapanmış stajı zaten eler.</para>
    /// </summary>
    public static async Task Handle(InternshipReplacementRequested e, IDocumentSession session)
    {
        var candidates = await session.Query<InternshipSummary>()
            .Where(s => s.StudentId == e.StudentId)
            .ToListAsync();

        var summary = candidates.FirstOrDefault(s => SagaCorrelationPolicy.MatchesContract(
            s.StudentId, s.BusinessId, s.Phase, e.StudentId, e.OldBusinessId));
        if (summary is null) return;

        summary.Phase = InternshipPhase.Terminated;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }
}
