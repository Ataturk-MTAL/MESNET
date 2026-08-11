using Marten;
using Microsoft.Extensions.Logging;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Application.Events;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Microsoft.Extensions.Caching.Memory;
using Wolverine;

namespace MESNET.Security.Application.Handlers;

public static class CreateUserHandler
{
    public static async Task<(Guid, OutgoingMessages)> Handle(
        CreateUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        // Kapsam kontrolü OLUŞTURMADA da yapılır (ADR-0003 adım 6). ChangeUserInstitution
        // korunuyordu ama oluşturma açıktı: kilitli kapının yanındaki açık pencere. Ölçüldü —
        // A okulunun müdürü, InstitutionId'yi gövdeye yazarak B okuluna kullanıcı yaratabiliyordu.
        // Yeni kullanıcının bugünkü bağı yoktur, o yüzden "current" null geçilir.
        if (!UserInstitutionScopePolicy.CanAssign(
                command.ActorInstitutionId, null, command.InstitutionId,
                command.ActorHasPlatformScope))
            throw new DomainException(SecurityErrors.InstitutionScopeNotAllowed());

        var kcResult = await keycloak.CreateUserAsync(
            command.Username, command.Email, command.FirstName, command.LastName,
            command.TemporaryPassword);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        var keycloakUserId = kcResult.Value;

        if (command.Roles.Count > 0)
        {
            var roleResult = await keycloak.AssignRealmRolesAsync(keycloakUserId, command.Roles);
            if (roleResult.IsFailure)
                throw new DomainException(roleResult.Error);
        }

        // Kapsam anahtarlarının HİÇBİRİ Keycloak'a yazılmaz: institution_id (ADR-0003 adım 2)
        // ve business_id (#229). Otorite aşağıdaki UserAccount kaydıdır; claim'ler oradan
        // üretilir. Keycloak'ta bir kopya bırakmak, ileride birinin o kopyayı yeniden otorite
        // sanmasına davetiye çıkarır — kapatılan yol tam olarak buydu.
        //
        // Öznitelik sözlüğü şimdilik yalnız branş kodları için duruyor.
        var attributes = new Dictionary<string, string>();

        if (attributes.Count > 0)
            await keycloak.SetUserAttributesAsync(keycloakUserId, attributes);

        // Alan (branş) kapsamı kayıt sırasında sabitlenir (#126). Boş liste geçerlidir —
        // müdür/müdür yardımcısı hiçbir alana bağlı değildir; öznitelik hiç yazılmaz.
        var branchCodes = NormalizeBranchCodes(command.BranchCodes);
        if (branchCodes.Count > 0)
            await keycloak.SetUserAttributeValuesAsync(keycloakUserId, BranchCodeClaims.ClaimType, branchCodes);

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            Username = command.Username,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Roles = command.Roles,
            InstitutionId = command.InstitutionId,
            BusinessId = command.BusinessId,
            BranchCodes = branchCodes
        };

        session.Store(account);

        // Birden çok cascading mesaj OutgoingMessages ile döndürülür — 3'lü tuple'da Wolverine
        // yalnız 1. elemanı yanıt olarak alıp diğerlerini sessizce düşürüyor (bkz. PaymentSaga).
        var messages = new OutgoingMessages
        {
            new UserCreated(
                account.Id, keycloakUserId, command.Username, account.FullName, command.Email,
                command.Roles, command.InstitutionId, command.BusinessId,
                command.Metadata ?? [])
        };

        // Denetim satırları yalnız kullanıcı kimliğini saklar; adı modüller bu olayla
        // besledikleri kendi UserNameView'larından çözer (#137).
        if (UserDisplayNameEvents.TryCreate(account) is { } displayName)
            messages.Add(displayName);

        return (account.Id, messages);
    }

    /// <summary>Boş/yinelenen kodları eler. Sonuç boş olabilir — bu bir hata değildir.</summary>
    internal static List<string> NormalizeBranchCodes(IEnumerable<string>? codes) =>
        (codes ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

/// <summary>
/// Kullanıcının alan (branş) kapsamını değiştirir (#126).
///
/// <para>Kayıt sırasında girilen bilgi sonradan değişebilir: kullanıcı alan şefi yapılabilir,
/// başka alana geçebilir ya da ikinci bir alandan sorumlu olabilir. Kapsam değişimi permission
/// cache'ini geçersiz kılar — aksi hâlde eski kapsam 5 dakika daha geçerli kalırdı.</para>
/// </summary>
public static class ChangeUserBranchesHandler
{
    public static async Task<UserBranchesChanged> Handle(
        ChangeUserBranches command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var previous = account.BranchCodes.ToList();
        var branchCodes = CreateUserHandler.NormalizeBranchCodes(command.BranchCodes);

        // Boş liste özniteliği siler — kapsamı kaldırmak geçerli bir işlemdir
        // (ör. alan şefliğinden müdür yardımcılığına geçen kullanıcı).
        var kcResult = await keycloak.SetUserAttributeValuesAsync(
            account.KeycloakUserId, BranchCodeClaims.ClaimType, branchCodes);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.BranchCodes = branchCodes;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserBranchesChanged(account.Id, account.KeycloakUserId, previous, branchCodes);
    }
}

/// <summary>
/// Kullanıcının kurum (kiracı) bağını değiştirir (ADR-0003 adım 2).
///
/// <para><b>Kiracı anahtarının tek yazma yolu budur.</b> Token'daki <c>institution_id</c>
/// claim'i artık hiç kabul edilmiyor, <c>SyncUsersFromKeycloak</c> de bu alanı yazmıyor;
/// geriye tek bir idari işlem kalıyor ve o işlem denetlenebilir bir olay üretiyor.</para>
///
/// <para><b>Keycloak'a öznitelik YAZILMAZ.</b> <c>ChangeUserBranches</c> alan kodlarını
/// "görünürlük için" Keycloak'a da yazar; kurum için bu bilinçli olarak yapılmaz. Kiracı
/// anahtarının Keycloak'ta bir kopyasının bulunması, ileride birinin o kopyayı yeniden
/// otorite sanmasına davetiye çıkarır — ADR-0003'ün kapattığı yol tam olarak budur.</para>
///
/// <para>Kapsam değişimi permission cache'ini geçersiz kılar; aksi hâlde çözülen bağ cache
/// süresi boyunca geçerli kalır ve kullanıcı erişmemesi gereken kurumun verisini görmeye
/// devam ederdi.</para>
/// </summary>
public static class ChangeUserInstitutionHandler
{
    public static async Task<UserInstitutionChanged> Handle(
        ChangeUserInstitution command, IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        if (!UserInstitutionScopePolicy.CanAssign(
                command.ActorInstitutionId, account.InstitutionId, command.InstitutionId))
            throw new DomainException(SecurityErrors.InstitutionScopeNotAllowed());

        var previous = account.InstitutionId;

        account.InstitutionId = command.InstitutionId;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserInstitutionChanged(
            account.Id, account.KeycloakUserId, previous, command.InstitutionId);
    }
}

/// <summary>
/// Velinin bağlı olduğu öğrencileri değiştirir (#174).
///
/// <para>Kapsam kaydı ve Keycloak özniteliği birlikte güncellenir. Öznitelik yalnız
/// <b>görünürlük</b> içindir — otoriter olan kayıttır ve
/// <c>PermissionClaimsTransformation</c> token'dan gelen değeri her istekte siler.</para>
/// </summary>
/// <summary>
/// İşletme kapsamını değiştirir (#229). Claim kayıttan üretildiği için bu uç, yanlış bağı
/// düzeltebilecek tek yoldur.
///
/// <para>Önbellek geçersiz kılınır; aksi hâlde kaldırılan bağ 5 dakika daha geçerli kalır ve
/// kullanıcı erişmemesi gereken işletmenin verisini görmeye devam ederdi.</para>
/// </summary>
public static class ChangeUserBusinessHandler
{
    public static async Task<UserBusinessChanged> Handle(
        ChangeUserBusiness command, IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var previous = account.BusinessId;

        account.BusinessId = command.BusinessId;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserBusinessChanged(
            account.Id, account.KeycloakUserId, previous, command.BusinessId);
    }
}

public static class ChangeUserStudentsHandler
{
    public static async Task<UserStudentsChanged> Handle(
        ChangeUserStudents command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var previous = account.LinkedStudentIds.ToList();
        var studentIds = ParentScopePolicy.Normalize(command.StudentIds);

        // Boş liste özniteliği siler — bağı kaldırmak geçerli bir işlemdir.
        var kcResult = await keycloak.SetUserAttributeValuesAsync(
            account.KeycloakUserId,
            LinkedStudentClaims.ClaimType,
            [.. studentIds.Select(id => id.ToString())]);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.LinkedStudentIds = studentIds;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        // Kapsam değişimi cache'i geçersiz kılmalı; aksi hâlde kaldırılan bağ cache süresi
        // boyunca geçerli kalır ve veli erişmemesi gereken öğrencinin verisini görmeye devam eder.
        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserStudentsChanged(account.Id, account.KeycloakUserId, previous, studentIds);
    }
}

public static class UpdateUserHandler
{
    public static async Task<OutgoingMessages> Handle(
        UpdateUser command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var kcResult = await keycloak.UpdateUserAsync(
            account.KeycloakUserId, command.Email, command.FirstName, command.LastName);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.Email = command.Email;
        account.FirstName = command.FirstName;
        account.LastName = command.LastName;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        var messages = new OutgoingMessages
        {
            new UserUpdated(account.Id, account.KeycloakUserId, account.FullName, command.Email)
        };

        // Ad değişmiş olabilir — denetim satırlarının gösterdiği ad bu olayla tazelenir (#137).
        if (UserDisplayNameEvents.TryCreate(account) is { } displayName)
            messages.Add(displayName);

        return messages;
    }
}

public static class ChangeUserRolesHandler
{
    public static async Task<UserRolesChanged> Handle(
        ChangeUserRoles command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var previousRoles = account.Roles.ToList();

        var rolesToRemove = previousRoles.Except(command.NewRoles).ToList();
        if (rolesToRemove.Count > 0)
        {
            var removeResult = await keycloak.RemoveRealmRolesAsync(account.KeycloakUserId, rolesToRemove);
            if (removeResult.IsFailure)
                throw new DomainException(removeResult.Error);
        }

        var rolesToAdd = command.NewRoles.Except(previousRoles).ToList();
        if (rolesToAdd.Count > 0)
        {
            var addResult = await keycloak.AssignRealmRolesAsync(account.KeycloakUserId, rolesToAdd);
            if (addResult.IsFailure)
                throw new DomainException(addResult.Error);
        }

        account.Roles = command.NewRoles;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserRolesChanged(account.Id, account.KeycloakUserId, previousRoles, command.NewRoles);
    }
}

public static class ChangeUserPermissionsHandler
{
    public static async Task<UserPermissionsChanged> Handle(
        ChangeUserPermissions command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        // Mutlak guardrail — kapsam muafiyeti izinleri hiçbir yapılandırmayla bireysel
        // atanamaz (#126). Yapılandırılabilir kapsam kontrolünden ÖNCE gelir, çünkü
        // yapılandırma (PUT /permission-scopes, user:roles:manage) bu kuralı gevşetememeli.
        var neverAssignable = command.DirectPermissions
            .Where(AssignablePermissionScope.NeverDirectlyAssignable.Contains)
            .ToList();
        if (neverAssignable.Count > 0)
            throw new DomainException(SecurityErrors.PermissionNeverDirectlyAssignable(
                string.Join(", ", neverAssignable)));

        // Guardrail — kullanıcının rol kapsamı dışındaki yetkiler direct olarak ATANAMAZ
        // (ör. işletme kullanıcısına kurum-yönetimi yetkisi verilemez). Kapsam YAPILANDIRILABILIR.
        var scope = await PermissionScopeHandler.LoadScopeAsync(session);
        var notAssignable = command.DirectPermissions
            .Where(p => !AssignablePermissionScope.CanAssign(scope, account.Roles, p))
            .ToList();
        if (notAssignable.Count > 0)
            throw new DomainException(SecurityErrors.PermissionNotAssignableToRole(
                string.Join(", ", account.Roles), string.Join(", ", notAssignable)));

        var attributes = new Dictionary<string, string>
        {
            ["direct_permissions"] = string.Join(",", command.DirectPermissions)
        };
        var kcResult = await keycloak.SetUserAttributesAsync(account.KeycloakUserId, attributes);
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.DirectPermissions = command.DirectPermissions;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserPermissionsChanged(account.Id, account.KeycloakUserId, command.DirectPermissions);
    }
}

public static class ToggleUserStatusHandler
{
    public static async Task<object> Handle(
        ToggleUserStatus command, IKeycloakAdminService keycloak,
        IDocumentSession session, IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        // Mezar taşı yönetim yüzeyinde yok sayılır (#210).
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var kcResult = await keycloak.SetUserEnabledAsync(account.KeycloakUserId, command.Enable);
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        account.IsEnabled = command.Enable;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return command.Enable
            ? new UserActivated(account.Id, account.KeycloakUserId)
            : (object)new UserDeactivated(account.Id, account.KeycloakUserId, command.Reason ?? "");
    }
}

public static class DeleteUserHandler
{
    /// <summary>
    /// Kullanıcıyı siler. Keycloak kaydı gerçekten silinir, <b>yerel kayıt mezar taşı olarak
    /// kalır</b> (#210).
    ///
    /// <para><b>Neden sert silinmiyor:</b> kayıt yoksa izin dönüşümü "bu kullanıcının kaydı
    /// henüz oluşturulmamış" sanıp <b>token yedeğine</b> düşer ve izinleri <c>realm_access</c>
    /// rollerinden yeniden türetir. Token imzalıdır, yalnız imza + <c>exp</c> ile doğrulanır —
    /// silinen kullanıcı token'ı sona erene kadar (realm'de 1800 sn) tam yetkiyle çalışırdı.</para>
    ///
    /// <para>Önbellek de temizlenir; yoksa engel 5 dakika gecikirdi.</para>
    /// </summary>
    public static async Task<UserDeleted> Handle(
        DeleteUser command, IKeycloakAdminService keycloak, IDocumentSession session,
        IMemoryCache cache)
    {
        var account = await session.LoadAsync<UserAccount>(command.UserAccountId);
        if (account is null || account.DeletedAt is not null)
            throw new DomainException(SecurityErrors.UserNotFound(command.UserAccountId));

        var kcResult = await keycloak.DeleteUserAsync(account.KeycloakUserId);
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        // Mezar taşı: kayıt bulunmaya devam etmeli ki erişim kesilebilsin. IsEnabled de
        // düşürülür — iki alan birbirinin yedeği değil, ayrı ayrı kapatır.
        account.DeletedAt = DateTime.UtcNow;
        account.IsEnabled = false;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

        return new UserDeleted(account.Id, account.KeycloakUserId);
    }
}

/// <summary>
/// Senkronizasyon sonucu — toplam/yeni/güncellenen sayıları.
/// </summary>
/// <param name="WithoutInstitution">
/// Senkronizasyon sonunda <b>kurum kapsamı olmayan</b> hesap sayısı (ADR-0003 adım 2).
/// Sync kiracı anahtarını Keycloak'tan kopyalamaz; dışarıdan gelen kullanıcı kapsamsız
/// doğar ve bağı idari bir işlem kurar. Sayı sıfır değilse iş bitmemiştir — bu alan o
/// boşluğu <b>görünür</b> kılmak içindir.
/// </param>
public sealed record SyncUsersResult(int Total, int Created, int Updated, int WithoutInstitution);

/// <summary>Yeniden yayın sonucu — kaç hesap için ad olayı üretildiği.</summary>
public sealed record ResyncUserDisplayNamesResult(int Total, int Published, int Skipped);

/// <summary>
/// Mevcut hesaplar için ad olaylarını yeniden yayınlar — modüllerin
/// <c>UserNameView</c> read-model'lerinin backfill'i (#137).
/// </summary>
public static class ResyncUserDisplayNamesHandler
{
    public static async Task<(ResyncUserDisplayNamesResult, OutgoingMessages)> Handle(
        ResyncUserDisplayNames command, IQuerySession session)
    {
        // Silinmiş hesaplar için ad olayı yayınlanmaz (#210).
        var accounts = await session.Query<UserAccount>().Where(u => u.DeletedAt == null).ToListAsync();

        var messages = new OutgoingMessages();
        foreach (var account in accounts)
        {
            if (UserDisplayNameEvents.TryCreate(account) is { } displayName)
                messages.Add(displayName);
        }

        // Atlananlar: Keycloak kimliği Guid olarak ayrıştırılamayan hesaplar. Hata değildir —
        // denetim satırı token'daki sub claim'ini saklar, o kimlikle eşleşmeyecek bir view
        // kaydı yalnız çöp olurdu.
        var result = new ResyncUserDisplayNamesResult(
            accounts.Count, messages.Count, accounts.Count - messages.Count);

        return (result, messages);
    }
}

public static class SyncUsersFromKeycloakHandler
{
    public static async Task<(SyncUsersResult, OutgoingMessages)> Handle(
        SyncUsersFromKeycloak command, IKeycloakAdminService keycloak, IDocumentSession session,
        IMemoryCache cache)
    {
        // Senkronizasyon eskiden hiç olay yayınlamıyordu; yalnız UserAccount yazıyordu.
        // Denetim adlarını modüllerin UserNameView'ına taşıyan tek kanal bu olay olduğu için
        // burada da yayınlanmalı — yoksa Keycloak'tan gelen kullanıcılar view'a hiç girmez (#137).
        var messages = new OutgoingMessages();
        var kcResult = await keycloak.GetUsersAsync();
        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        // Mezar taşları eşleşmeye girmez; Keycloak'ta o kullanıcı zaten yok (#210).
        var existing = await session.Query<UserAccount>().Where(u => u.DeletedAt == null).ToListAsync();
        var byKeycloakId = existing
            .GroupBy(u => u.KeycloakUserId)
            .ToDictionary(g => g.Key, g => g.First());

        // Kapsamsız hesaplar SAYILIR ve uçta operatöre bildirilir. Sessiz kalırsa "sync
        // çalıştı" sanılır ve kullanıcıların neden kurum kapsamı göremediği aranmaya başlanır.
        int created = 0, updated = 0, withoutInstitution = 0;
        foreach (var ku in kcResult.Value)
        {
            if (byKeycloakId.TryGetValue(ku.Id, out var account))
            {
                // Keycloak kaynak — temel alanları + rolleri tazele (lokal kurum/işletme bağı korunur, KC'de varsa güncellenir)
                account.Username = ku.Username;
                account.Email = ku.Email;
                account.FirstName = ku.FirstName;
                account.LastName = ku.LastName;
                account.IsEnabled = ku.Enabled;
                account.Roles = ku.Roles;
                // InstitutionId BİLEREK tazelenmez (ADR-0003 adım 2): kiracı anahtarının
                // otoritesi bu kayıttır, Keycloak değil. Eski satır
                // (`if (ku.InstitutionId.HasValue) account.InstitutionId = ku.InstitutionId;`)
                // Keycloak özniteliğini kaydın üzerine yazıyordu — yani kullanıcının
                // yazabileceği bir kaynağı otorite hâline getiriyordu. Kurum bağı yalnız
                // ChangeUserInstitution ile değişir.
                // İşletme kapsamı Keycloak'tan KOPYALANMAZ (#229). Kullanıcının yazabileceği
                // bir öznitelik, senkronizasyondan geçerek "otoriter kayıt" hâline gelirdi —
                // #224'te kurum için kaldırılan aklama deseninin aynısı. Bağ idari bir
                // işlemdir: ChangeUserBusiness.
                // Branş yalnız Keycloak'ta VARSA tazelenir; yoksa lokal kayıt korunur.
                // Boş öznitelik "branşı sil" anlamına gelmez — sync uydurmaz da, silmez de (#126).
                if (ku.BranchCodes.Count > 0) account.BranchCodes = ku.BranchCodes;
                account.UpdatedAt = DateTime.UtcNow;
                session.Store(account);

                // Rol/kapsam tazelendi — önbellek temizlenmezse izinler 5 dakika daha ESKİ
                // hâliyle verilir (#209). Ölçüldü: düzeltmeden sonra uç 180 saniye boyunca 403
                // dönmeye devam etti; yönetici düzeltmenin işe yaramadığını sanar.
                //
                // Yeni hesap dalında gerekmez: kaydı bulunamayan kullanıcı için dönüşüm
                // önbelleğe hiçbir şey koymaz, bayatlayacak girdi yoktur.
                PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);

                if (UserDisplayNameEvents.TryCreate(account) is { } updatedName)
                    messages.Add(updatedName);
                updated++;
                if (HasNoInstitution(account.InstitutionId)) withoutInstitution++;
            }
            else
            {
                var newAccount = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    KeycloakUserId = ku.Id,
                    Username = ku.Username,
                    Email = ku.Email,
                    FirstName = ku.FirstName,
                    LastName = ku.LastName,
                    IsEnabled = ku.Enabled,
                    Roles = ku.Roles,
                    // Kiracı anahtarı Keycloak'tan KOPYALANMAZ (ADR-0003 adım 2). Dışarıdan
                    // gelen kullanıcı kurum kapsamsız doğar; bağ idari bir işlemdir
                    // (ChangeUserInstitution) ya da personel kaydından backfill'lenir.
                    InstitutionId = null,
                    // Dışarıdan gelen kullanıcı işletme kapsamsız doğar (#229); kurum
                    // anahtarıyla aynı gerekçe (#224).
                    BusinessId = null,
                    BranchCodes = ku.BranchCodes
                };
                session.Store(newAccount);
                if (UserDisplayNameEvents.TryCreate(newAccount) is { } createdName)
                    messages.Add(createdName);
                created++;
                withoutInstitution++;   // yeni hesap her zaman kapsamsız doğar
            }
        }

        return (new SyncUsersResult(kcResult.Value.Count, created, updated, withoutInstitution), messages);
    }

    private static bool HasNoInstitution(Guid? institutionId) =>
        institutionId is null || institutionId == Guid.Empty;
}

/// <summary>
/// <see cref="PurgeKeycloakInstitutionAttribute"/> sonucu. <c>Skipped</c> özniteliği zaten
/// olmayan kullanıcılardır; <c>Failed</c> ise Keycloak'ın reddettikleri — <b>sıfırdan farklıysa
/// bakılmalıdır</b>, çünkü o kullanıcılarda artık duruyor demektir.
/// </summary>
public sealed record PurgeInstitutionAttributeResult(int Total, int Purged, int Skipped, int Failed);

/// <summary>
/// Keycloak'taki artık <c>institution_id</c> özniteliğini siler (ADR-0003 adım 3).
///
/// <para><b>Neden ayrı bir uç:</b> öznitelik atıldır ama kendiliğinden kaybolmaz. Silme işi
/// Keycloak'ta tek tek elle yapılırsa <b>yanlış yapılır</b>: yalnız <c>{"attributes": {...}}</c>
/// göndermek kullanıcının <c>firstName</c>/<c>email</c> alanlarını 204 dönerek siler (#190).
/// Buradaki yol öznitelik yazan normal yoldan geçer, gövdeyi taze bir GET'ten kurar.</para>
///
/// <para><b>Idempotenttir:</b> ikinci koşuda hiçbir kullanıcıda öznitelik kalmadığı için
/// <c>Purged = 0</c> döner.</para>
/// </summary>
public static class PurgeKeycloakInstitutionAttributeHandler
{
    /// <summary>
    /// Keycloak'ta artık kalan <b>kapsam anahtarı</b> öznitelikleri. İkisi de otoritesini
    /// <c>UserAccount</c>'a bıraktı — <c>institution_id</c> ADR-0003 adım 2 ile,
    /// <c>business_id</c> #229 ile — ve hiçbir kod artık onları yazmıyor.
    /// </summary>
    private static readonly string[] ScopeAttributes = ["institution_id", "business_id"];

    public static async Task<PurgeInstitutionAttributeResult> Handle(
        PurgeKeycloakInstitutionAttribute command,
        IKeycloakAdminService keycloak,
        ILogger<PurgeKeycloakInstitutionAttribute> logger,
        CancellationToken ct)
    {
        var users = await keycloak.GetUsersAsync(ct);
        if (users.IsFailure)
            throw new DomainException(users.Error);

        int purged = 0, skipped = 0, failed = 0;

        foreach (var user in users.Value)
        {
            if (user.InstitutionId is null && user.BusinessId is null)
            {
                skipped++;
                continue;
            }

            var userFailed = false;
            foreach (var attribute in ScopeAttributes)
            {
                // Boş değer listesi özniteliği kaldırır; diğer öznitelikler ve profil alanları
                // korunur (gövde taze bir GET'ten kurulur — bkz. KeycloakUserWritePolicy).
                var result = await keycloak.SetUserAttributeValuesAsync(user.Id, attribute, [], ct);
                if (result.IsFailure)
                {
                    userFailed = true;
                    logger.LogWarning(
                        "{Attribute} özniteliği silinemedi: {Username} ({UserId}) — {Error}",
                        attribute, user.Username, user.Id, result.Error.Description);
                }
            }

            if (userFailed) { failed++; continue; }

            purged++;
        }

        logger.LogInformation(
            "Keycloak institution_id temizliği: {Total} kullanıcı, {Purged} silindi, "
            + "{Skipped} zaten yoktu, {Failed} başarısız.",
            users.Value.Count, purged, skipped, failed);

        return new PurgeInstitutionAttributeResult(users.Value.Count, purged, skipped, failed);
    }
}
