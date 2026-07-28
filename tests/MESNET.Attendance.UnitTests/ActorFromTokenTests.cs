using System.Reflection;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Handlers;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Denetim aktörünün <b>kimlik olarak</b> saklandığı regresyonu (#139).
///
/// <para><b>Bu #137'den farklıdır ve karıştırılmamalıdır.</b> #137'de aktör istek
/// gövdesinden okunuyordu — istemci kendi denetim aktörünü yazabiliyordu, yani sahte aktör
/// basılabiliyordu. Attendance'ta böyle bir açık hiç olmadı: üç alan da
/// <c>ICurrentUserService</c> üzerinden token'dan geliyordu. Buradaki sorun daha küçüktü ve
/// üç başlıktaydı: kullanıcı adını değiştirince eski kayıtların <b>bayat ad</b> göstermesi,
/// aktöre göre <b>sorgulanamama</b> (aynı adlı iki kullanıcı ayrılamaz) ve diğer beş modülle
/// <b>desen tutarsızlığı</b>.</para>
///
/// <para>Çözüm aynı: kimlik saklanır, ad okuma anında <c>UserNameView</c>'dan çözülür.</para>
/// </summary>
public sealed class ActorFromTokenTests
{
    /// <summary>Serbest metin ad saklaması YASAK olan alan adları.</summary>
    private static readonly string[] ActorNameFields =
    [
        "MarkedBy", "ApprovedBy", "VerifiedBy", "UpdatedBy",
        "MarkedByName", "ApprovedByName", "VerifiedByName", "UpdatedByName",
    ];

    private static IEnumerable<Type> CommandTypes =>
        typeof(MarkAttendance).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace == typeof(MarkAttendance).Namespace);

    [Fact]
    public void Hicbir_komut_aktor_adi_tasimaz()
    {
        var offenders = CommandTypes
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => ActorNameFields.Contains(p.Name))
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        offenders.ShouldBeEmpty(
            "Aktör alanı ad değil kimlik taşımalıdır (#139). "
            + $"İhlal eden: {string.Join(", ", offenders)}");
    }

    [Theory]
    [InlineData(typeof(MarkAttendanceHandler))]
    [InlineData(typeof(ApproveAttendanceHandler))]
    [InlineData(typeof(VerifyAttendanceHandler))]
    [InlineData(typeof(UpdateWorkCalendarHandler))]
    public void Aktor_damgalayan_handlerlar_token_servisini_alir(Type handler)
    {
        var takesCurrentUser = handler
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.Name is "Handle" or "HandleAsync")
            .SelectMany(m => m.GetParameters())
            .Any(p => p.ParameterType == typeof(ICurrentUserService));

        takesCurrentUser.ShouldBeTrue(
            $"{handler.Name} denetim aktörü yazar; aktör token'dan alınmak ZORUNDADIR (#139).");
    }

    [Fact]
    public void Saklanan_aktor_alanlari_kimliktir_ad_degildir()
    {
        typeof(AttendanceRecord).GetProperty("MarkedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(AttendanceRecord).GetProperty("ApprovedById")!.PropertyType.ShouldBe(typeof(Guid?));
        typeof(AttendanceRecord).GetProperty("VerifiedById")!.PropertyType.ShouldBe(typeof(Guid?));

        typeof(AttendanceMarked).GetProperty("MarkedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(AttendanceApproved).GetProperty("ApprovedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(AttendanceVerified).GetProperty("VerifiedById")!.PropertyType.ShouldBe(typeof(Guid));

        typeof(WorkCalendar).GetProperty("UpdatedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(WorkCalendarUpdated).GetProperty("UpdatedById")!.PropertyType.ShouldBe(typeof(Guid));
    }

    /// <summary>
    /// Saklanan olay alanları YENİDEN ADLANDIRILMIŞ olmalıdır — tipi yerinde değiştirmek
    /// veri kaybından öte veri <b>bozulması</b> demektir.
    ///
    /// <para><c>AttendanceMarked</c> / <c>AttendanceApproved</c> / <c>AttendanceVerified</c>
    /// <c>shared.mt_events</c> içinde kalıcıdır ve <c>AttendanceRecord</c> her okumada
    /// bunlardan replay edilir. Alan adı <c>MarkedBy</c> kalıp tipi <c>Guid</c> yapılsaydı,
    /// saklı <c>"markedBy": "Ahmet Yılmaz"</c> deserialize edilemez ve replay
    /// <c>JsonException</c> ile TÜMDEN kırılırdı — yalnız denetim adı değil, devamsızlık
    /// kaydının kendisi okunamaz hâle gelirdi. Yeni ad eski anahtarı sessizce yok sayar.</para>
    /// </summary>
    [Fact]
    public void Eski_serbest_metin_aktor_alanlari_geri_gelmez()
    {
        typeof(AttendanceMarked).GetProperty("MarkedBy").ShouldBeNull(
            "Alan yeniden adlandırıldı; eski adı geri koymak saklı event JSON'unu "
            + "okunamaz kılar ve aggregate replay'ini kırar (#139).");

        typeof(AttendanceApproved).GetProperty("ApprovedBy").ShouldBeNull();
        typeof(AttendanceVerified).GetProperty("VerifiedBy").ShouldBeNull();

        typeof(AttendanceRecord).GetProperty("MarkedBy").ShouldBeNull();
        typeof(AttendanceRecord).GetProperty("ApprovedBy").ShouldBeNull();
        typeof(AttendanceRecord).GetProperty("VerifiedBy").ShouldBeNull();

        typeof(WorkCalendar).GetProperty("UpdatedBy").ShouldBeNull();
        typeof(WorkCalendarUpdated).GetProperty("UpdatedBy").ShouldBeNull();
    }

    /// <summary>
    /// Bildirim mesajı ad DEĞİL kimlik taşır (#139). Mesaj Wolverine durable local queue'ya
    /// konur ve tüketilene kadar <c>wolverine</c> şemasında bekler; ad burada taşınsaydı,
    /// kuyrukta beklerken kullanıcı adını değiştirdiğinde bildirim eski adı gösterirdi.
    /// </summary>
    [Fact]
    public void Bildirim_mesaji_kimlik_tasir_ad_tasimaz()
    {
        typeof(NotifyAttendancePendingApproval).GetProperty("MarkedByName").ShouldBeNull();
        typeof(NotifyAttendancePendingApproval).GetProperty("MarkedById")!
            .PropertyType.ShouldBe(typeof(Guid));
    }
}
