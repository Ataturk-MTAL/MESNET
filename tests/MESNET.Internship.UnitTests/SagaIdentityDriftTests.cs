using System.Reflection;
using MESNET.Internship.Application.Sagas;
using Shouldly;
using Wolverine.Persistence.Sagas;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Saga'nın her giriş noktası kimliğini <b>çözülebilir</b> taşımalı (#248).
///
/// <para><b>Yaşanan:</b> Wolverine saga kimliğini mesajın alan ADINDAN çözer. Internship'in
/// kendi mesajları <c>InternshipId</c> taşıdığı için çalışıyordu; <b>başka modülün dört
/// olayı</b> (<c>ContractActivated</c>, <c>ContractTerminated</c>, <c>ContractCompleted</c>,
/// <c>AttendanceLimitExceeded</c>) taşımadığı için <c>IndeterminateSagaStateIdException</c> ile
/// ölü mektup kuyruğuna düşüyordu.</para>
///
/// <para><b>Hiçbiri hata olarak görünmüyordu.</b> Uçlar 2xx dönüyor, olaylar yayınlanıyor,
/// sonra sessizce kayboluyordu. Canlı ölçüm: <b>2248 saga'nın hiçbirinde</b> <c>contractId</c>
/// yazılı değildi ve 2139'u sözleşme aktifken bile <c>AwaitingContract</c>'ta çakılıydı.
/// Devamsızlık sınırı hiç fesih başlatmamıştı.</para>
///
/// <para><b>Neden derleyici yakalamıyor:</b> imza geçerli, tip doğru, kod derleniyor. Kırılan
/// şey bir <i>isim konvansiyonu</i>; yalnız çalışma zamanında, yalnız o yol tetiklendiğinde
/// belli oluyor. Bu yüzden kural teste kilitlendi.</para>
///
/// <para><b>Aynı tuzak PaymentSaga'da da yaşanmıştı</b> ve orada
/// <c>[SagaIdentityFrom(nameof(...))]</c> ile çözülmüştü — bu test o çözümü de geçerli sayar.</para>
/// </summary>
public sealed class SagaIdentityDriftTests
{
    private static readonly Type SagaType = typeof(InternshipSaga);

    /// <summary>
    /// Wolverine'in tanıdığı adlar. <c>InternshipSaga</c> için hem <c>InternshipSagaId</c> hem
    /// <c>InternshipId</c> geçerlidir (tip adının <c>Saga</c> soneki düşürülür).
    /// </summary>
    private static readonly string[] AcceptedNames =
    [
        "Id",
        "SagaId",
        $"{SagaType.Name}Id",
        $"{SagaType.Name.Replace("Saga", "")}Id",
    ];

    public static TheoryData<string, Type> SagaHandlerMessages()
    {
        var data = new TheoryData<string, Type>();

        foreach (var method in SagaType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                     .Where(m => m.Name is "Handle" or "Handles" or "Consume"))
        {
            var parameter = method.GetParameters().FirstOrDefault();
            if (parameter is null) continue;

            data.Add($"{method.Name}({parameter.ParameterType.Name})", parameter.ParameterType);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SagaHandlerMessages))]
    public void Saga_giris_noktasi_kimligi_cozulebilir_tasir(string _, Type messageType)
    {
        var hasIdentityProperty = messageType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.PropertyType == typeof(Guid) && AcceptedNames.Contains(p.Name));

        var hasIdentityAttribute = SagaType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(m => m.GetParameters())
            .Where(p => p.ParameterType == messageType)
            .Any(p => p.GetCustomAttribute<SagaIdentityFromAttribute>() is not null);

        (hasIdentityProperty || hasIdentityAttribute).ShouldBeTrue(
            $"{messageType.Name} saga kimliğini çözülebilir taşımıyor. Kabul edilen adlardan biri "
            + $"({string.Join(", ", AcceptedNames)}) Guid alan olarak bulunmalı ya da parametre "
            + "[SagaIdentityFrom] taşımalı. Aksi hâlde mesaj IndeterminateSagaStateIdException "
            + "ile ölü mektuba düşer ve bu HATA OLARAK GÖRÜNMEZ.");
    }

    /// <summary>
    /// <b>Başka modülün olayı saga'ya doğrudan bağlanamaz.</b> Yukarıdaki testi geçmek için
    /// birinin <c>Contract.Shared</c>'deki bir olaya <c>InternshipId</c> eklemesi yeterdi —
    /// ama o, Internship'in iç kimliğini komşu modüle sızdırmak olurdu. Çeviri
    /// <c>SagaRelayConsumer</c>'da kalmalı.
    /// </summary>
    [Theory]
    [MemberData(nameof(SagaHandlerMessages))]
    public void Saga_yalniz_kendi_modulunun_mesajlarini_dinler(string _, Type messageType)
    {
        var assembly = messageType.Assembly.GetName().Name ?? "";

        assembly.StartsWith("MESNET.Internship", StringComparison.Ordinal).ShouldBeTrue(
            $"{messageType.Name} {assembly} içinde tanımlı. Saga başka modülün mesajını doğrudan "
            + "dinlerse ya kimliği çözülemez (ölü mektup) ya da o modüle Internship'in kimlik "
            + "kavramı sızar. Aktarıcı ekleyin: SagaRelayConsumer.");
    }
}
