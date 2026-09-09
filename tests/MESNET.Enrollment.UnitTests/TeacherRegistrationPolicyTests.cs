using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Öğretmen kaydının idempotanlığı (#310).
///
/// <para>Ölçüldü: handler koşulsuz <c>Guid.NewGuid()</c> ile kayıt açtığı için 6 öğretmenin
/// 2'şer kopyası doğdu ve <c>/api/teachers</c> 12 satır döndürdü. Doğal anahtar
/// <c>(InstitutionId, KeycloakUserId)</c>'dir: bir Keycloak kullanıcısının aynı kurumda iki
/// öğretmen profili olamaz.</para>
/// </summary>
public class TeacherRegistrationPolicyTests
{
    private static readonly Guid Ataturk = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");
    private static readonly Guid Gazi = Guid.Parse("a24ebbab-8c58-4373-b936-640fa3247e77");

    private static TeacherProfile Teacher(Guid institutionId, Guid keycloakUserId, string name = "Ayşe Çelik") => new()
    {
        Id = Guid.NewGuid(),
        InstitutionId = institutionId,
        KeycloakUserId = keycloakUserId,
        FullName = name,
        BranchCode = "EET"
    };

    [Fact]
    public void Ayni_kurumdaki_ayni_keycloak_kullanicisi_mevcut_profili_dondurur()
    {
        // Arrange
        var kcId = Guid.NewGuid();
        var mevcut = Teacher(Ataturk, kcId);

        // Act
        var bulunan = TeacherRegistrationPolicy.FindExisting([mevcut], Ataturk, kcId);

        // Assert
        bulunan.ShouldNotBeNull();
        bulunan.Id.ShouldBe(mevcut.Id);
    }

    [Fact]
    public void Baska_kurumdaki_ayni_kullanici_eslesmez()
    {
        // Arrange — aynı kişi, başka okulda: ayrı profil hakkı vardır
        var kcId = Guid.NewGuid();
        var gaziKaydi = Teacher(Gazi, kcId);

        // Act
        var bulunan = TeacherRegistrationPolicy.FindExisting([gaziKaydi], Ataturk, kcId);

        // Assert
        bulunan.ShouldBeNull();
    }

    [Fact]
    public void Farkli_keycloak_kullanicisi_eslesmez()
    {
        // Arrange
        var mevcut = Teacher(Ataturk, Guid.NewGuid());

        // Act
        var bulunan = TeacherRegistrationPolicy.FindExisting([mevcut], Ataturk, Guid.NewGuid());

        // Assert
        bulunan.ShouldBeNull();
    }

    [Fact]
    public void Anahtarsiz_kayit_eslesmez_hepsi_tek_profile_cokmemelidir()
    {
        // Arrange — KeycloakUserId boşsa doğal anahtar YOKTUR. Eşleştirilirse anahtarsız
        // bütün öğretmenler tek profile çöker; mükerrerlikten beter bir hata olur.
        var anahtarsiz = Teacher(Ataturk, Guid.Empty, "Mustafa Yılmaz");

        // Act
        var bulunan = TeacherRegistrationPolicy.FindExisting([anahtarsiz], Ataturk, Guid.Empty);

        // Assert
        bulunan.ShouldBeNull();
    }

    [Fact]
    public void Mukerrer_kayit_varsa_en_eski_profil_dondurulur()
    {
        // Arrange — mevcut mükerrerler temizlenene kadar sonuç KARARLI olmalı: her çağrı
        // aynı kaydı seçmeli, yoksa atamalar iki profil arasında salınır.
        var kcId = Guid.NewGuid();
        var eski = Teacher(Ataturk, kcId);
        eski.RegisteredAt = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var yeni = Teacher(Ataturk, kcId);
        yeni.RegisteredAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var bulunan = TeacherRegistrationPolicy.FindExisting([yeni, eski], Ataturk, kcId);

        // Assert
        bulunan!.Id.ShouldBe(eski.Id);
    }
}
