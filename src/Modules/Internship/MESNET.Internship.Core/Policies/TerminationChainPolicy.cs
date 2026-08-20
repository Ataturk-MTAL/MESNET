using Ardalis.SmartEnum;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Core.ValueObjects;

namespace MESNET.Internship.Core.Policies;

/// <summary>
/// Fesih onay zincirinin bir adımı (#218).
///
/// <para>Zincirde <b>üç adım</b> vardır: koordinatör öğretmen → müdür yardımcısı → müdür.
/// Veli ve işletme yetkilisi fesih <b>talep eder</b>, onaylamaz; onların adımları kaldırıldı.
/// Kalsalardı arayüz olmayan bir yetkiyi varmış gibi gösterirdi.</para>
///
/// <para>Her adım kendi <b>ucunu</b> ve <b>iznini</b> taşır; arayüz butonu buna bakar.</para>
/// </summary>
public sealed class TerminationStep : SmartEnum<TerminationStep>
{
    public static readonly TerminationStep Teacher =
        new(nameof(Teacher), 1, "Koordinatör Öğretmen", "teacher", Permissions.Internship.Approve);

    public static readonly TerminationStep Deputy =
        new(nameof(Deputy), 2, "Müdür Yardımcısı", "deputy", Permissions.Internship.Approve);

    public static readonly TerminationStep Director =
        new(nameof(Director), 3, "Müdür", "director", Permissions.Internship.Manage);

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
/// Zincirde sıradaki adımı belirler ve <b>sırayı dayatır</b> (#218).
///
/// <para><b>Sıra zorunludur:</b> müdür yardımcısı, öğretmen onaylamadan onaylayamaz. Eski model
/// her onayı bağımsız bir bayrak olarak yazıyordu; <c>business-rules.md</c> §4.3 "sırayla"
/// diyordu ama kuralın kodda karşılığı yoktu.</para>
///
/// <para><b>Bu sınıf saftır</b> — karar burada, uygulaması saga'da. Aynı ayrım
/// <c>PlacementScopePolicy</c> ve <c>RealmInvariants</c>'ta da var.</para>
/// </summary>
public static class TerminationChainPolicy
{
    /// <summary>Zincirin sırası. Görüntüleme değil, <b>dayatma</b> sırasıdır.</summary>
    private static readonly TerminationStep[] Order =
    [
        TerminationStep.Teacher,
        TerminationStep.Deputy,
        TerminationStep.Director
    ];

    /// <summary>
    /// Sıradaki adım; zincir kapandıysa ya da hiç başlamadıysa <c>null</c>.
    /// </summary>
    /// <param name="chain">Zincir; <c>null</c> ise fesih süreci açılmamıştır.</param>
    public static TerminationStep? NextStep(TerminationApprovalChain? chain)
    {
        if (chain is null) return null;

        // Override zinciri tümüyle kapatır — eksik adımlar artık beklenmiyor.
        if (chain.IsOverridden) return null;

        return Order.FirstOrDefault(step => !IsApproved(chain, step));
    }

    /// <summary>
    /// Bu adım <b>şimdi</b> onaylanabilir mi. Sırası gelmemiş ya da geçmiş adım için
    /// <c>false</c> döner.
    /// </summary>
    public static bool CanApprove(TerminationApprovalChain? chain, TerminationStep step) =>
        NextStep(chain) == step;

    /// <summary>
    /// Sırası gelmemiş adımın reddine konacak açıklama — hangi adımın beklendiğini söyler,
    /// yoksa kullanıcı neyi beklediğini bilemez.
    /// </summary>
    public static string DescribeOutOfOrder(TerminationApprovalChain? chain, TerminationStep step)
    {
        var expected = NextStep(chain);

        if (expected is null)
            return $"Fesih onay zinciri kapalı; '{step.Slug}' adımı onaylanamaz.";

        return $"Onay sırası: önce '{expected.Slug}' adımı tamamlanmalı. "
             + $"'{step.Slug}' adımının sırası henüz gelmedi.";
    }

    /// <summary>
    /// Fesih onay zinciri <b>şimdi başlatılabilir mi</b> (#252).
    ///
    /// <para><b>Yürüyen zincir yeniden başlatılmaz.</b> Saga zinciri koşulsuz kuruyordu
    /// (<c>ApprovalChain = new(...)</c>); ikinci bir fesih talebi toplanmış öğretmen / müdür
    /// yardımcısı / müdür onaylarını <b>sessizce siliyordu</b>. Talebi üreten iki yol da
    /// tekrarlanabilir: aktarıcı her <c>AttendanceLimitExceeded</c>'de bir tane üretir ve sayaç
    /// dönem içinde <b>sıfırlanmadığı</b> için sınır dolduktan sonraki her kayıt/onay yeniden
    /// tetikler; manuel uç da ikinci kez çağrılabilir.</para>
    ///
    /// <para><b>Zincir <c>null</c>'a geri dönmez:</b> ne tamamlanma, ne override, ne de
    /// fesih onu temizler — dolayısıyla "zincir var" = "bu staj için fesih süreci bir kez
    /// başlatılmış" demektir ve tek kontrol yeterlidir.</para>
    ///
    /// <para><b>Neden faz değil zincir sorulur:</b> <c>SagaCorrelationPolicy.IsOpen</c>
    /// <c>TerminationInProgress</c>'i bilerek <b>açık</b> sayar (sözleşme olayları zincirin
    /// devamıdır) ve daraltılamaz — daraltılırsa <c>ContractTerminated</c> saga'yı bulamaz,
    /// staj sonsuza kadar fesih sürecinde asılı kalır.</para>
    /// </summary>
    public static bool CanStart(TerminationApprovalChain? chain) => chain is null;

    private static bool IsApproved(TerminationApprovalChain chain, TerminationStep step) =>
        step.Name switch
        {
            nameof(TerminationStep.Teacher) => chain.TeacherApproved,
            nameof(TerminationStep.Deputy) => chain.DeputyApproved,
            nameof(TerminationStep.Director) => chain.DirectorApproved,
            _ => throw new ArgumentOutOfRangeException(
                nameof(step), step.Name, "Tanınmayan fesih onay adımı.")
        };
}
