namespace MESNET.Payment.Application.Queries;

/// <summary>
/// Devlet katkısı bloke olan öğrenciler — sınıf tekrarı nedeniyle bu sınıf yılı için katkı
/// yeniden hesaplanmayacak olanlar (#161).
/// </summary>
/// <remarks>
/// Yerleştirme ve sözleşme ekranları bunu okur: işletme, maliyetinin yükseleceğini <b>ayın
/// sonunda dekont gelirken değil</b>, öğrenciyi kabul ederken bilmelidir. Aksi hâlde "neden bu
/// ay katkı gelmedi" sorusu destek çağrısına dönüşür ve işletmenin güveni zedelenir.
///
/// <para>Liste küçük ve tümü tek seferde okunur (yerleştirme ekranında lookup) — sayfalama
/// bilinçli olarak yok.</para>
/// </remarks>
public sealed record GetContributionBlockedStudents;

/// <param name="StudentId">Öğrenci.</param>
/// <param name="ClassYear">Tekrar edilen ve katkısı tükenmiş sınıf yılı.</param>
/// <param name="FirstClaimedMonth">Katkının ilk alındığı ay (<c>yyyy-MM</c>) — gerekçe izi.</param>
public sealed record ContributionBlockedStudentDto(Guid StudentId, int ClassYear, string FirstClaimedMonth);

/// <remarks>
/// Wolverine <c>IEnumerable&lt;T&gt;</c> dönüşünü cascading mesaj sayar ve koleksiyonu
/// çağırana DÖNDÜRMEZ — sonuç bu yüzden somut bir kayda sarılır (CLAUDE.md tuzağı).
/// </remarks>
public sealed record ContributionBlockedStudentsResult(List<ContributionBlockedStudentDto> Items);
