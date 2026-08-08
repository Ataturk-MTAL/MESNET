using Ardalis.SmartEnum;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Core.ValueObjects;

namespace MESNET.Internship.Core.Policies;

/// <summary>
/// Fesih onay zincirinin bir adımı (#191).
///
/// <para>Her adım kendi <b>ucunu</b> ve <b>iznini</b> taşır. Arayüz butonu hangi kullanıcıya
/// göstereceğine buradan karar verir; iki ayrı yere yazılsaydı biri değişip diğeri
/// unutulduğunda buton yanlış kişiye görünürdü — ve yanlış görünen buton, sunucu reddettiği
/// için yalnız kafa karıştırır.</para>
/// </summary>
public sealed class TerminationStep : SmartEnum<TerminationStep>
{
    public static readonly TerminationStep Parent =
        new(nameof(Parent), 1, "Veli", "parent", Permissions.Internship.ApproveParent);

    public static readonly TerminationStep Teacher =
        new(nameof(Teacher), 2, "Koordinatör Öğretmen", "teacher", Permissions.Internship.Approve);

    public static readonly TerminationStep Deputy =
        new(nameof(Deputy), 3, "Müdür Yardımcısı", "deputy", Permissions.Internship.Approve);

    public static readonly TerminationStep Director =
        new(nameof(Director), 4, "Müdür", "director", Permissions.Internship.Manage);

    public static readonly TerminationStep BusinessRep =
        new(nameof(BusinessRep), 5, "İşletme Yetkilisi", "business", Permissions.Company.Student);

    /// <summary>Türkçe görünen ad.</summary>
    public string Slug { get; }

    /// <summary><c>POST /api/internships/{id}/approve/{Endpoint}</c> yolundaki son parça.</summary>
    public string Endpoint { get; }

    /// <summary>Bu adımı yapabilmek için gereken izin.</summary>
    public string Permission { get; }

    private TerminationStep(string name, int value, string slug, string endpoint, string permission)
        : base(name, value)
    {
        Slug = slug;
        Endpoint = endpoint;
        Permission = permission;
    }
}

/// <summary>
/// Zincirde hangi adımların beklediğini hesaplar (#191).
///
/// <para><b>Zincir SIRALI DEĞİLDİR.</b> Saga her onayı bağımsız bir bayrak olarak yazar
/// (<c>ApprovalChain with { DirectorApproved = true }</c>); müdür, öğretmenden önce
/// onaylayabilir ve bu geçerlidir. Bu yüzden politika "sıradaki adım" değil <b>bekleyen
/// adımların kümesini</b> döndürür. Sıra dayatmak, gerçekte olabilen bir durumu "imkânsız"
/// saymak olurdu.</para>
///
/// <para><b>Bu sınıf saftır</b> — karar burada, okuma sorgu handler'ında. Aynı ayrım
/// <c>PlacementScopePolicy</c> ve <c>RealmInvariants</c>'ta da var.</para>
///
/// <para><b>Bilinen tutarsızlık (#159 etkileşimi):</b> <c>TerminationApprovalChain.IsComplete</c>
/// işletme onayını <b>her zaman</b> arar. Okulda staj yapan (işverensiz) öğrencide işletme
/// yetkilisi yoktur, dolayısıyla o zincir kendiliğinden hiç tamamlanamaz — tek çıkış override.
/// Politika bunu <b>düzeltmez</b>, backend gerçeğini birebir yansıtır: aksi hâlde arayüz
/// "bekleyen adım yok" derken saga süreci kapatmaz ve iki taraf birbirini yalanlardı.</para>
/// </summary>
public static class TerminationChainPolicy
{
    /// <summary>Zincirin kanonik adım sırası — görüntüleme içindir, dayatma değildir.</summary>
    private static readonly TerminationStep[] KanonikSira =
    [
        TerminationStep.Parent,
        TerminationStep.Teacher,
        TerminationStep.Deputy,
        TerminationStep.Director,
        TerminationStep.BusinessRep
    ];

    /// <summary>
    /// Henüz onaylanmamış adımlar. Boş liste = beklenen adım yok.
    /// </summary>
    /// <param name="chain">Zincir; <c>null</c> ise fesih süreci hiç açılmamıştır.</param>
    /// <param name="requiresParent">
    /// Veli onayı aranıyor mu — kararı saga verir (<c>RequiresParentApproval</c>), politika
    /// yeniden üretmez, uygular.
    /// </param>
    public static IReadOnlyList<TerminationStep> PendingSteps(
        TerminationApprovalChain? chain, bool requiresParent)
    {
        // Süreç açılmamış: "hepsi bekliyor" demek olmayan bir süreci varmış gibi gösterirdi.
        if (chain is null) return [];

        // Override zinciri tümüyle kapatır; eksik adımlar artık beklenmiyor.
        if (chain.IsOverridden) return [];

        return [.. KanonikSira.Where(step => !Onaylandi(chain, step, requiresParent))];
    }

    private static bool Onaylandi(TerminationApprovalChain chain, TerminationStep step, bool requiresParent) =>
        step.Name switch
        {
            nameof(TerminationStep.Parent) => !requiresParent || chain.ParentApproved,
            nameof(TerminationStep.Teacher) => chain.TeacherApproved,
            nameof(TerminationStep.Deputy) => chain.DeputyApproved,
            nameof(TerminationStep.Director) => chain.DirectorApproved,
            nameof(TerminationStep.BusinessRep) => chain.BusinessRepApproved,
            _ => throw new ArgumentOutOfRangeException(
                nameof(step), step.Name, "Tanınmayan fesih onay adımı.")
        };
}
