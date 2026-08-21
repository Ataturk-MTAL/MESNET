using System.Reflection;
using System.Text.RegularExpressions;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Email;
using MESNET.Security.Application.Consumers;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kademeli tebligatın <b>e-posta</b> ayağı (#247 3/3) — md. 36 (4)'ün "yazılı bildirim"
/// gereğini karşılayan tek kanal.
///
/// <para><b>Neden Security'de:</b> alıcı adresleri <c>UserAccount.Email</c>'de. Attendance oraya
/// sorgu atamaz ve adresi olayda taşımak kişisel veriyi olay akışına kalıcı yazmak olurdu.</para>
/// </summary>
public sealed class AbsenceNotificationEmailTests
{
    private static readonly string Kaynak = StripComments(File.ReadAllText(ConsumerSourcePath()));

    [Fact]
    public void Tebligat_olayi_eposta_icin_tuketiliyor()
    {
        typeof(AbsenceNotificationEmailConsumer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name is "Consume" or "ConsumeAsync")
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .ShouldContain(typeof(AbsenceNotificationDue));
    }

    /// <summary>E-posta arayüzü bildirim gönderimini destekliyor — davet dışında ikinci yol.</summary>
    [Fact]
    public void Eposta_arayuzu_bildirim_gonderimini_destekliyor()
    {
        typeof(IEmailService)
            .GetMethod("SendAbsenceNotificationEmailAsync")
            .ShouldNotBeNull();
    }

    /// <summary>
    /// Üç alıcı grubu da çözülmeli: veli, işletme ve (18+ ise) öğrencinin kendisi.
    /// </summary>
    [Theory]
    [InlineData("LinkedStudentIds.Contains", "veli")]
    [InlineData("u.BusinessId == @event.BusinessId", "işletme")]
    [InlineData("@event.NotifyStudent && u.StudentId", "öğrenci (18+)")]
    public void Alici_gruplari_cozuluyor(string desen, string grup)
    {
        Kaynak.Contains(desen, StringComparison.Ordinal)
            .ShouldBeTrue($"'{grup}' alıcı grubu çözülmüyor.");
    }

    /// <summary>
    /// Bir kullanıcı hem veli hem işletme yetkilisi olabilir; aynı tebligatı iki kez almamalı.
    /// </summary>
    [Fact]
    public void Alicilar_tekillestiriliyor()
    {
        Kaynak.Contains("DistinctBy", StringComparison.Ordinal).ShouldBeTrue();
    }

    /// <summary>Devre dışı hesaplara tebligat gönderilmez.</summary>
    [Fact]
    public void Devre_disi_hesaplara_gonderilmez()
    {
        Kaynak.Contains("u.IsEnabled", StringComparison.Ordinal).ShouldBeTrue();
    }

    /// <summary>
    /// <b>Alıcı bulunamazsa sessiz kalınmaz.</b> Md. 36 (4) bir yükümlülüktür; en yaygın sebep
    /// veli bağının hiç kurulmamış olmasıdır ve bu bugün elle yapılıyor.
    /// </summary>
    [Fact]
    public void Alici_bulunamazsa_uyari_loglanir()
    {
        Kaynak.Contains("HİÇ ALICI BULUNAMADI", StringComparison.Ordinal).ShouldBeTrue(
            "Yerine getirilemeyen tebligat yükümlülüğünün izi olmalı.");
    }

    /// <summary>Gönderim başarısızlığı da yutulmaz.</summary>
    [Fact]
    public void Gonderim_hatasi_loglanir()
    {
        Kaynak.Contains("result.IsFailure", StringComparison.Ordinal).ShouldBeTrue();
    }


    /// <summary>
    /// <b>Yorumlar ayıklanır.</b> İlk sürüm tüm dosyayı tarıyordu ve XML doc yorumlarındaki
    /// <c>GuardianOfStudentIds</c> kelimesi testi yanlış yeşile çeviriyordu: hedefleme boyutu
    /// yanlış olana çevrildiğinde bile test geçiyordu. Ölçüldü.
    /// </summary>
    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        var withoutXmlDoc = Regex.Replace(withoutBlocks, @"^\s*///.*$", string.Empty, RegexOptions.Multiline);
        return Regex.Replace(withoutXmlDoc, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    private static string ConsumerSourcePath() => Path.Combine(RepoRoot(),
        "src/Modules/Security/MESNET.Security.Application/Consumers/AbsenceNotificationEmailConsumer.cs");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx).");
    }
}
