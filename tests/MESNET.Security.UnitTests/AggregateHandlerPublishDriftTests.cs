using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <c>[AggregateHandler]</c> dönüşü <b>cascading mesaj değildir</b> — tüketicisi olan olay
/// ayrıca <b>yayınlanmak zorundadır</b> (#253).
///
/// <para><b>Mekanizma:</b> Wolverine.Marten aggregate iş akışında handler zincirinin
/// <c>ReturnVariableActionSource</c>'unu <c>EventCaptureActionSource</c> ile değiştirir; o kaynak
/// yalnız <c>IEventStream&lt;T&gt;.AppendOne</c> frame'i üretir. Dönen olay Marten akışına
/// yazılır ama <b>hiçbir Wolverine tüketicisine yönlendirilmez</b>. Olay yönlendirmesi
/// (<c>EventForwardingToWolverine</c>) bu projede kapalıdır ve açılmamalıdır: elle yayınlanan
/// olaylar (ör. <c>AttendanceMarked</c>) çift işlenirdi.</para>
///
/// <para><b>Neden test:</b> bu hata <b>sessizdir</b>. Kod derlenir, birim testler yeşil kalır,
/// dead letter kuyruğunda iz yoktur — tüketici basitçe hiç çağrılmaz. Ölçüldü: dev
/// veritabanında <c>attendance_corrected</c> olayları akışta vardı ama Payment'ın
/// <c>AbsenceTallyConsumer</c> aşırı yüklemeleri hiç çalışmamıştı; <c>ContractActivated</c> için
/// de aynısı geçerliydi ve <c>InternshipSaga</c> <c>AwaitingContract</c>'ta çakılı kalıyordu.
/// Tek çalışan yol elden tetiklenen resync backfill'iydi.</para>
///
/// <para><b>Neden imza taraması yetmiyor:</b> #252'de yazılan ilk kilit "bu olay için handler var
/// mı" diye soruyordu ve gerçek açığı <b>yanlış yeşille</b> geçirdi. Doğru soru "olay
/// <b>yönlendiriliyor</b> mu". Bu test onu dönüş tipinden okur.</para>
///
/// <para><b>Neden kaynak taraması, reflection değil:</b> kural depo geneliyle ilgilidir ve tek
/// bir test projesinin tüm modüllerin <c>Application</c> katmanlarını referans etmesi csproj
/// kuralına aykırıdır (<c>InstitutionScopeDriftTests</c> ile aynı sınır).</para>
///
/// <para><b>Sınır — dürüstçe:</b> tarama dönüş <b>tipini</b> görür, çalışma zamanında o
/// koleksiyona gerçekten olay konduğunu göremez. Boş bir <c>OutgoingMessages</c> dönüşü bu
/// testten geçer.</para>
/// </summary>
public sealed class AggregateHandlerPublishDriftTests
{
    /// <summary><c>[AggregateHandler]</c> ile işaretli metodun bildirimi — dönüş tipi yakalanır.</summary>
    private static readonly Regex AggregateHandlerMethod = new(
        @"\[AggregateHandler\][^\]]*?\s+public\s+static\s+(?<ret>[^\n]+?)\s+(?:Handle|HandleAsync|Consume|ConsumeAsync)\s*\(",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>Bir mesajı ilk parametre olarak alan handler/consumer metodu.</summary>
    private static readonly Regex ConsumerMethod = new(
        @"(?:Handle|HandleAsync|Consume|ConsumeAsync)\s*\(\s*(?:\[[^\]]*\]\s*)?(?<msg>\w+)\s+@?\w+",
        RegexOptions.Compiled);

    /// <summary>
    /// <b>Bilerek dondurulan borç.</b> Liste yalnız <b>küçülebilir</b>; büyürse test kırmızı olur
    /// ve refleks tersine döner — yeni bir <c>[AggregateHandler]</c> tüketicisi olan olayı
    /// yayınlamadan geçemez.
    ///
    /// <para><b>Liste şu an BOŞ</b> — #254 ile son satır da düştü
    /// (<c>ReviewPaidLeaveHandler</c> → <c>PaidLeaveApproved</c>). Boş kalması hedeftir; buraya
    /// satır eklemek, sessizce çalışmayan bir tüketiciyi kabul etmek demektir ve gerekçesi
    /// yazılmalıdır.</para>
    /// </summary>
    private static readonly HashSet<string> KnownDebt = new(StringComparer.Ordinal);

    [Fact]
    public void Tuketicisi_olan_olay_aggregate_handler_tarafindan_yayinlanmali()
    {
        var kaynaklar = ApplicationSources();
        kaynaklar.ShouldNotBeEmpty("Application katmanı kaynakları bulunamadı — tarama boşa çalışıyor.");

        var tuketilenler = ConsumedMessageNames(kaynaklar);
        tuketilenler.ShouldNotBeEmpty("Hiç tüketici bulunamadı — regex bozulmuş olabilir.");

        var ihlaller = new List<string>();

        foreach (var (yol, metin) in kaynaklar)
        {
            var dosya = Path.GetFileName(yol);

            foreach (Match eslesme in AggregateHandlerMethod.Matches(metin))
            {
                var donus = eslesme.Groups["ret"].Value;

                // Olayı yayınlayan doğru şekil: (Events, OutgoingMessages) — ya da onun Task'ı.
                if (donus.Contains("OutgoingMessages", StringComparison.Ordinal)) continue;

                foreach (var olay in DonulenOlaylar(donus))
                {
                    if (!tuketilenler.Contains(olay)) continue;

                    var anahtar = $"{dosya}:{olay}";
                    if (KnownDebt.Contains(anahtar)) continue;

                    ihlaller.Add(anahtar);
                }
            }
        }

        ihlaller.ShouldBeEmpty(
            "Bu [AggregateHandler] metotları tüketicisi OLAN bir olay döndürüyor ama olayı "
            + "yayınlamıyor; olay yalnız Marten akışına yazılır ve tüketici sessizce hiç "
            + "çalışmaz. Doğru şekil: (Events, OutgoingMessages). "
            + $"İhlaller: {string.Join(", ", ihlaller)}");
    }

    /// <summary>Dondurulan borç yalnız küçülebilir — kaydı düşen satır listeden çıkarılmalı.</summary>
    [Fact]
    public void Dondurulan_borc_hala_gecerli()
    {
        var kaynaklar = ApplicationSources();
        var tuketilenler = ConsumedMessageNames(kaynaklar);

        foreach (var anahtar in KnownDebt)
        {
            var parcalar = anahtar.Split(':');
            var dosya = parcalar[0];
            var olay = parcalar[1];

            var kaynak = kaynaklar.FirstOrDefault(k => Path.GetFileName(k.Yol) == dosya);
            kaynak.Yol.ShouldNotBeNull(
                $"Dondurulan borçtaki dosya artık yok: {dosya}. Satırı listeden çıkarın.");

            tuketilenler.ShouldContain(olay,
                $"'{olay}' artık hiçbir yerde tüketilmiyor; '{anahtar}' satırı listeden çıkarılmalı.");

            kaynak.Metin.Contains("OutgoingMessages", StringComparison.Ordinal).ShouldBeFalse(
                $"'{dosya}' artık olayı yayınlıyor — '{anahtar}' satırı listeden ÇIKARILMALI, "
                + "yoksa borç listesi düzeltilmiş bir hatayı gizlemeye devam eder.");
        }
    }

    /// <summary>
    /// Dönüş tipinden olay adlarını çıkarır. Tekil olay (<c>ContractActivated</c>),
    /// <c>Task&lt;X&gt;</c> ve tuple ayakları desteklenir; <c>object</c> gibi belirsiz dönüşler
    /// atlanır — hangi olayın döndüğü kaynaktan okunamaz.
    /// </summary>
    private static IEnumerable<string> DonulenOlaylar(string donus)
    {
        var temiz = donus.Trim().TrimEnd('?');

        if (temiz.StartsWith("async ", StringComparison.Ordinal))
            temiz = temiz["async ".Length..].Trim();

        if (temiz.StartsWith("Task<", StringComparison.Ordinal) && temiz.EndsWith('>'))
            temiz = temiz[5..^1].Trim().TrimEnd('?');

        var ayaklar = temiz.StartsWith('(') && temiz.EndsWith(')')
            ? temiz[1..^1].Split(',')
            : [temiz];

        foreach (var ayak in ayaklar)
        {
            var ad = ayak.Trim().TrimEnd('?');

            // Belirsiz ya da olay olmayan dönüşler.
            if (ad is "object" or "void" or "Task" or "Events" or "OutgoingMessages" or "") continue;
            if (!char.IsUpper(ad[0])) continue;

            yield return ad;
        }
    }

    /// <summary>Depodaki her modülün <c>Application</c> katmanındaki kaynak dosyalar.</summary>
    private static List<(string Yol, string Metin)> ApplicationSources()
    {
        var modulesRoot = Path.Combine(RepoRoot(), "src", "Modules");

        return Directory
            .EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => p.Contains(".Application", StringComparison.Ordinal))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(p => (Yol: p, Metin: File.ReadAllText(p)))
            .ToList();
    }

    /// <summary>
    /// Bir handler/consumer metodunun <b>ilk parametresi</b> olarak geçen tip adları — yani
    /// fiilen tüketilen mesaj tipleri. <c>[AggregateHandler]</c> metotlarının kendi komut
    /// parametreleri de listeye girer; zararsızdır, çünkü karşılaştırma <b>olay</b> adlarıyla
    /// yapılır ve komut adı bir olay dönüşüyle çakışmaz.
    /// </summary>
    private static HashSet<string> ConsumedMessageNames(List<(string Yol, string Metin)> kaynaklar)
    {
        var adlar = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (_, metin) in kaynaklar)
            foreach (Match eslesme in ConsumerMethod.Matches(metin))
                adlar.Add(eslesme.Groups["msg"].Value);

        return adlar;
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Depo kökü bulunamadı (MESNET.slnx aranıyordu): {AppContext.BaseDirectory}");
    }
}
