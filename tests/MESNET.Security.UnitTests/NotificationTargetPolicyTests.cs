using MESNET.Common.Infrastructure.Notifications;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Bildirim kime ulaşır (#247) — hedeflemenin <b>ilk</b> kilitleyici testi.
///
/// <para><b>Neden şimdiye kadar test yoktu ve neden tehlikeli:</b> hedefleme sessiz bir
/// yüzeydir. Hedef kitle boş çıkarsa <c>SseNotificationService</c> yalnız <c>LogDebug</c> yazıp
/// döner — yanlış hedefleme ne hata verir, ne dead letter'a düşer, ne log'da göze çarpar.
/// Karar <c>SseConnectionManager</c> içinde private bir metottu ve depoda <c>MatchesTarget</c>
/// geçen tek bir test yoktu.</para>
///
/// <para><b>Bulunan kırık:</b> <c>NotificationTarget.StudentIds</c> yalnız <c>user.StudentId</c>
/// ile eşleşiyordu — yani öğrencinin <b>kendisine</b> ulaşıyor, <b>velisine ulaşmıyordu</b>.
/// Veli–öğrenci bağı (#174) SSE tarafında hiç taşınmıyordu. Md. 36 (4)'ün "veliye bildirim"
/// zorunluluğu bu altyapıyla karşılanamazdı ve bu sessiz kalırdı.</para>
/// </summary>
public sealed class NotificationTargetPolicyTests
{
    private static readonly Guid Ogrenci = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BaskaOgrenci = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isletme = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid BaskaIsletme = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Kurum = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static SseUserContext Kullanici(
        Guid? studentId = null, Guid? businessId = null, Guid? institutionId = null,
        IReadOnlyList<Guid>? linkedStudentIds = null,
        IReadOnlyList<string>? roles = null, IReadOnlyList<string>? permissions = null)
        => new(
            UserId: Guid.NewGuid(),
            FullName: "Deneme Kullanıcı",
            InstitutionId: institutionId,
            BusinessId: businessId,
            StudentId: studentId,
            Roles: roles ?? [],
            Permissions: permissions ?? [],
            LinkedStudentIds: linkedStudentIds);

    // ─── Veli boyutu (#247'nin asıl kırığı) ──────────────────────────────────────────

    /// <summary><b>Asıl regresyon.</b> Veli, bağlı olduğu öğrencinin bildirimini alır.</summary>
    [Fact]
    public void Veli_bagli_ogrencinin_bildirimini_alir()
    {
        var veli = Kullanici(linkedStudentIds: [Ogrenci]);

        NotificationTargetPolicy.Matches(veli, new NotificationTarget { GuardianOfStudentIds = [Ogrenci] })
            .ShouldBeTrue("Md. 36 (4) veliye bildirim zorunlu kılıyor.");
    }

    [Fact]
    public void Veli_baska_ogrencinin_bildirimini_almaz()
    {
        var veli = Kullanici(linkedStudentIds: [BaskaOgrenci]);

        NotificationTargetPolicy.Matches(veli, new NotificationTarget { GuardianOfStudentIds = [Ogrenci] })
            .ShouldBeFalse();
    }

    /// <summary>
    /// <b>Boyut ayrımı kritik:</b> <c>StudentIds</c> veliye ulaşmaz. Md. 36 (4) öğrenciye yalnız
    /// 18 yaşını doldurmuşsa bildirim istiyor; tek boyut olsaydı 18 altı öğrenci velisinin
    /// tebligatını da görürdü.
    /// </summary>
    [Fact]
    public void StudentIds_veliye_ulasmaz()
    {
        var veli = Kullanici(linkedStudentIds: [Ogrenci]);

        NotificationTargetPolicy.Matches(veli, new NotificationTarget { StudentIds = [Ogrenci] })
            .ShouldBeFalse("Veli ve öğrenci ayrı hedeflenebilmeli.");
    }

    /// <summary>Tersi de doğru: öğrencinin kendisi veli boyutuyla hedeflenmez.</summary>
    [Fact]
    public void GuardianOfStudentIds_ogrencinin_kendisine_ulasmaz()
    {
        var ogrenci = Kullanici(studentId: Ogrenci);

        NotificationTargetPolicy.Matches(ogrenci, new NotificationTarget { GuardianOfStudentIds = [Ogrenci] })
            .ShouldBeFalse();
    }

    [Fact]
    public void Ogrenci_kendi_bildirimini_alir()
    {
        var ogrenci = Kullanici(studentId: Ogrenci);

        NotificationTargetPolicy.Matches(ogrenci, new NotificationTarget { StudentIds = [Ogrenci] })
            .ShouldBeTrue();
    }

    /// <summary>Bağı olmayan kullanıcı hiçbir veli bildirimini almaz — boş liste erişim doğurmaz.</summary>
    [Fact]
    public void Bagi_olmayan_kullanici_veli_bildirimi_almaz()
    {
        NotificationTargetPolicy.Matches(Kullanici(), new NotificationTarget { GuardianOfStudentIds = [Ogrenci] })
            .ShouldBeFalse();
    }

    // ─── İşletme boyutu ──────────────────────────────────────────────────────────────

    [Fact]
    public void Isletme_kullanicisi_kendi_isletmesinin_bildirimini_alir()
    {
        NotificationTargetPolicy.Matches(
                Kullanici(businessId: Isletme), new NotificationTarget { BusinessIds = [Isletme] })
            .ShouldBeTrue();
    }

    [Fact]
    public void Isletme_kullanicisi_baska_isletmenin_bildirimini_almaz()
    {
        NotificationTargetPolicy.Matches(
                Kullanici(businessId: BaskaIsletme), new NotificationTarget { BusinessIds = [Isletme] })
            .ShouldBeFalse("Tebligat yanlış işletmeye düşemez.");
    }

    /// <summary>
    /// Okulda staj hâlinde (#159) işletme yoktur. Boş kimlik hedefle eşleşmemeli — aksi hâlde
    /// işletmesiz kullanıcılar birbirinin bildirimini alırdı.
    /// </summary>
    [Fact]
    public void Bos_isletme_kimligi_eslesmez()
    {
        NotificationTargetPolicy.Matches(
                Kullanici(businessId: Guid.Empty), new NotificationTarget { BusinessIds = [Guid.Empty] })
            .ShouldBeFalse();
    }

    [Fact]
    public void Isletmesi_olmayan_kullanici_isletme_bildirimi_almaz()
    {
        NotificationTargetPolicy.Matches(Kullanici(), new NotificationTarget { BusinessIds = [Isletme] })
            .ShouldBeFalse();
    }

    // ─── Mevcut boyutlar korunuyor ───────────────────────────────────────────────────

    [Fact]
    public void Kurum_eslesmesi_calisiyor()
    {
        NotificationTargetPolicy.Matches(
                Kullanici(institutionId: Kurum), new NotificationTarget { InstitutionId = Kurum })
            .ShouldBeTrue();
    }

    [Fact]
    public void Bos_hedef_kimseye_ulasmaz()
    {
        NotificationTargetPolicy.Matches(
                Kullanici(studentId: Ogrenci, businessId: Isletme, institutionId: Kurum),
                new NotificationTarget())
            .ShouldBeFalse();
    }

    /// <summary>
    /// <b>Bilinen sınır — #266.</b> Ölçütler OR mantığındadır: <c>Roles</c> ve
    /// <c>RequiredPermission</c> kiracı sınırını KORUMAZ. Bu test o davranışı <b>onaylamıyor</b>,
    /// var olduğunu kayda geçiriyor; düzeltme #266'da. Davranış değişince bu test kırılacak ve
    /// bu istenen sonuçtur.
    /// </summary>
    [Fact]
    public void BILINEN_SINIR_rol_olcutu_kurum_suzgecini_etkisiz_kiliyor()
    {
        var baskaOkulunMuduru = Kullanici(
            institutionId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
            roles: ["InstitutionManager"]);

        NotificationTargetPolicy.Matches(
                baskaOkulunMuduru,
                new NotificationTarget { InstitutionId = Kurum, Roles = ["InstitutionManager"] })
            .ShouldBeTrue("#266: OR mantığı kurum süzgecini etkisiz kılıyor — kayda geçirilmiş sızıntı.");
    }
}
