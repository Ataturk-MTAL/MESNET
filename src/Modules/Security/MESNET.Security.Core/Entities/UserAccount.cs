namespace MESNET.Security.Core.Entities;

/// <summary>
/// Keycloak kullanıcısının yerel gölge kopyası.
/// Document storage — CRUD ağırlıklı, event sourcing kullanılmaz.
/// </summary>
public class UserAccount
{
    public Guid Id { get; set; }
    public required string KeycloakUserId { get; init; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public bool IsEnabled { get; set; } = true;
    public Guid? InstitutionId { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? StudentId { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> DirectPermissions { get; set; } = [];

    /// <summary>
    /// Kullanıcının sorumlu olduğu alan (branş) kodları (#126).
    /// <b>Kayıt sırasında girilir</b>, sistem tarafından türetilmez — `branch_codes` claim'inin
    /// kaynağıdır. Boş olması hata değildir: müdür/müdür yardımcısı hiçbir alana bağlı değildir.
    /// </summary>
    public List<string> BranchCodes { get; set; } = [];

    /// <summary>
    /// Velinin bağlı olduğu öğrenciler (#174) — <c>linked_student_ids</c> claim'inin kaynağı.
    ///
    /// <para><b>Kapsam buradan okunur, izinden DEĞİL</b> (ADR-0001). Tüm velilerin izinleri
    /// aynıdır; onları birbirinden ayıran tek şey bu listedir. Boş olması normaldir — veli
    /// olmayan her kullanıcıda boştur ve hiçbir öğrenciye erişim doğurmaz.</para>
    ///
    /// <para>Bir veli birden çok öğrenciye (kardeşler) bağlı olabilir; bu yüzden
    /// <see cref="StudentId"/> (öğrencinin kendi hesabı) ile karıştırılmamalıdır.</para>
    /// </summary>
    public List<Guid> LinkedStudentIds { get; set; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Silinme damgası — <b>mezar taşı</b> (#210). <c>null</c> = silinmemiş.
    ///
    /// <para><b>Kayıt neden silinmiyor:</b> silinirse izin dönüşümü onu bulamaz, "kaydı henüz
    /// oluşturulmamış" sanar ve <b>token yedeğine</b> düşerek izinleri <c>realm_access</c>
    /// rollerinden yeniden türetir. Token imzalıdır ve yalnız imza + <c>exp</c> ile doğrulanır
    /// (introspection yok) — yani silinen kullanıcı, token'ı sona erene kadar tam yetkiyle
    /// çalışırdı. Realm'de <c>accessTokenLifespan: 1800</c>, yani 30 dakika.</para>
    ///
    /// <para>Damgalı kayıt yönetim yüzeyinde <b>görünmez</b>: listeler, tarama ve yükleme
    /// yolları onu yok sayar. Yalnız izin dönüşümü onu bulur — erişimi kesmek için.</para>
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
