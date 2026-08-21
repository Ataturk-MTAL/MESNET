using System.Reflection;
using System.Text.RegularExpressions;
using MESNET.Attendance.Application.Consumers;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Notifications;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Kademeli tebligatın <b>uygulama içi</b> teslimatı (#247 3/3) — üç alıcı, üç hedefleme boyutu.
///
/// <para><b>Neden yapısal test:</b> hedefleme sessiz bir yüzeydir — hedef kitle boş çıkarsa
/// servis yalnız <c>LogDebug</c> yazar. Yanlış boyut kullanmak "bildirim gitti" görüntüsü verir
/// ama kimseye ulaşmaz.</para>
/// </summary>
public sealed class AbsenceNotificationDeliveryTests
{
    private static readonly string Kaynak = StripComments(File.ReadAllText(ConsumerSourcePath()));

    [Fact]
    public void Tebligat_olayi_tuketiliyor()
    {
        typeof(AbsenceNotificationConsumer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name is "Consume" or "ConsumeAsync")
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .ShouldContain(typeof(AbsenceNotificationDue));
    }

    /// <summary>
    /// <b>Veli boyutu doğru olmalı.</b> <c>StudentIds</c> veliye ULAŞMAZ (#247 1/3) — yanlış
    /// boyut kullanılırsa md. 36 (4)'ün koşulsuz veli bildirimi sessizce hiç gitmez.
    /// </summary>
    [Fact]
    public void Veli_dogru_boyutla_hedefleniyor()
    {
        Kaynak.Contains("GuardianOfStudentIds", StringComparison.Ordinal).ShouldBeTrue(
            "Veli StudentIds ile hedeflenemez; o boyut yalnız öğrencinin kendisine ulaşır.");
    }

    [Fact]
    public void Isletme_dogru_boyutla_hedefleniyor()
    {
        Kaynak.Contains("BusinessIds", StringComparison.Ordinal).ShouldBeTrue();
    }

    /// <summary>
    /// Öğrenci ayağı <b>koşulludur</b> — md. 36 (4) yalnız 18 yaşını doldurmuş öğrenciye
    /// istiyor. Koşulsuz gönderilseydi 18 altı öğrenci velisinin tebligatını görürdü.
    /// </summary>
    [Fact]
    public void Ogrenci_ayagi_kosula_bagli()
    {
        Kaynak.Contains("@event.NotifyStudent", StringComparison.Ordinal).ShouldBeTrue(
            "Öğrenciye bildirim yalnız 18+ hâlinde gider.");
    }

    /// <summary>
    /// Okulda stajda (#159) işletme yoktur. Boş kimlik hedeflenirse işletmesiz tüm kullanıcılar
    /// birbirinin tebligatını alırdı.
    /// </summary>
    [Fact]
    public void Bos_isletme_kimligi_hedeflenmez()
    {
        Kaynak.Contains("BusinessId != Guid.Empty", StringComparison.Ordinal).ShouldBeTrue();
    }

    /// <summary>
    /// Veli ve işletme ayakları <b>koşulsuzdur</b>: hiçbir <c>if</c> onları
    /// <c>NotifyStudent</c>'a bağlamamalı.
    /// </summary>
    [Fact]
    public void Veli_ayagi_kosulsuz()
    {
        // Veli yayını, NotifyStudent koşulunun AÇILDIĞI yerden önce olmalı.
        Kaynak.IndexOf("GuardianOfStudentIds", StringComparison.Ordinal)
            .ShouldBeLessThan(Kaynak.IndexOf("@event.NotifyStudent", StringComparison.Ordinal),
                "Veli ayağı koşulsuzdur ve öğrenci koşulunun içine düşmemeli.");
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
        "src/Modules/Attendance/MESNET.Attendance.Application/Consumers/AbsenceNotificationConsumer.cs");

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
