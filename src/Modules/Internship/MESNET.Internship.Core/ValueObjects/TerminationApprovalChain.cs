namespace MESNET.Internship.Core.ValueObjects;

/// <summary>
/// Fesih onay zinciri: <b>koordinatör öğretmen → müdür yardımcısı → müdür</b> (#218).
///
/// <para><b>Veli ve işletme yetkilisi bu zincirde YOKTUR</b> — onlar fesih <b>talep eder</b>,
/// onaylamaz. Talebi kimin açtığı <c>RequestedBy</c>/<c>ReasonType</c> ile kaydedilir.</para>
///
/// <list type="bullet">
///   <item>İşletme ya da veli fesih isterse: öğretmen → müdür yrd. → müdür; <b>müdür onayında
///   fesih tamamlanır.</b></item>
///   <item>Okul tek taraflı fesih edecekse: koordinatör öğretmen talep eder, müdür yrd. ve
///   müdür onaylar.</item>
/// </list>
///
/// <para><b>Neden değişti:</b> model beş onaycı sayıyordu ve <c>IsComplete</c> işletme onayını
/// <b>koşulsuz</b> arıyordu. Okulda staj yapan (işverensiz, #159) öğrencinin işletmesi
/// olmadığı için o zincir kendiliğinden hiç kapanmıyordu — tek çıkış override'dı ve her okulda
/// staj fesihinde override kaydı doğuyordu, bu da override'ın anlamını ("zincir takıldı")
/// aşındırıyordu.</para>
/// </summary>
public sealed record TerminationApprovalChain
{
    public bool TeacherApproved { get; init; }
    public bool DeputyApproved { get; init; }
    public bool DirectorApproved { get; init; }
    public bool IsOverridden { get; init; }
    public string? OverriddenBy { get; init; }
    public DateTime? OverriddenAt { get; init; }
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Üç onay da alındı mı.
    /// </summary>
    /// <remarks>
    /// Parametresizdir: eski imza <c>requiresParent</c> alıyordu, çünkü veli bir onaycıydı.
    /// Artık değil — koşullu adım kalmadı.
    /// </remarks>
    public bool IsComplete() => TeacherApproved && DeputyApproved && DirectorApproved;

    public bool IsCompleteOrOverridden() => IsComplete() || IsOverridden;
}
