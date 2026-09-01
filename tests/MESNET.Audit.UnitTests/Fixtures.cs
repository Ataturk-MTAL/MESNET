// Denetim testleri gerçek komut AD ALANI şeklini taklit eden tipler ister; Audit modülü
// hiçbir modülü referans etmediği için (ve etmemeli), örnekler burada tanımlanır.
//
// AD ALANLARI KASITLI OLARAK "MESNET.AuditFixtures.Sample.*"tır, "MESNET.Attendance.*" DEĞİL:
// Görev 4'te bu test projesi on modülün Application assembly'sini referans edecek ve gerçek
// MESNET.Attendance.Application.Commands.MarkAttendance ile çakışırdı (CS0433). Şimdi doğru
// adı koymak, sonra taşımaktan ucuzdur.
//
// Dosya-kapsamlı ad alanı (namespace X;) bir dosyada yalnız BİR kez kullanılabilir; üç ad
// alanı olduğu için BLOK gövdeli yazılır.

namespace MESNET.AuditFixtures.Sample.Application.Commands
{
    public sealed record MarkAttendanceSample(Guid StudentId, Guid ContractId);
    public sealed record CorrectAttendanceSample(Guid AttendanceId);

    /// <summary>Commands/ klasörüne yanlış yerleşmiş bir SORGUYU taklit eder (Görev 4).</summary>
    public sealed record GetUserAccountsSample(int Page);
}

namespace MESNET.AuditFixtures.Sample.Application.Queries
{
    public sealed record GetAttendanceSample(Guid AttendanceId);
}

namespace MESNET.AuditFixtures.Sample.Application.Consumers
{
    public sealed record AttendanceMarkedSample(Guid AttendanceId);
}
