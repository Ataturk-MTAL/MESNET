---
title: Kullanıcı Kayıt Akışı
---

# Kullanıcı Onboarding — Davet Süreci

Kullanıcılar sisteme **davet akışı** ile eklenir. Keycloak doğrudan yönetim paneli üzerinden değil, Security modülünün davet mekanizması aracılığıyla beslenir.

---

## Genel Bakış

```
Kurum Yöneticisi        Sistem                  Davet Edilen Kullanıcı
      │                    │                              │
      │── Davet oluştur ──▶│                              │
      │                    │ (PendingApproval)            │
      │── Onayla ──────────▶│                              │
      │                    │ (Approved + e-posta gönder) ─▶│
      │                    │                              │
      │                    │◀── Kullanıcı adı + şifre ───│
      │                    │ (Keycloak + DB oluşturulur)  │
      │                    │ (Completed)                  │
```

---

## Adım Adım Akış

### 1. Davet Oluşturma

**Endpoint:** `POST /api/security/invitations`
**İzin:** `usermanagement:create`
**Kim yapar:** Kurum yöneticisi

Gönderilen veriler:
- `email` — Davet edilecek kişinin e-posta adresi
- `firstName`, `lastName` — Ad soyad
- `targetRole` — Atanacak rol (`Teacher`, `Student`, `InstitutionAdmin`, `CompanyManager` vb.)
- `institutionId` — Hangi kuruma ait olduğu
- `businessId` — (opsiyonel) İşletme kapsamlı roller için
- `metadata` — Role özgü ek bilgiler (öğrenci için `branchCode`, öğretmen için `staffRole` vb.)

Sonuç: Veritabanında `PendingApproval` durumunda `UserInvitation` kaydı oluşur. `InvitationCreated` eventi yayınlanır.

---

### 2. Onaylama (veya Reddetme)

**Endpoint:** `POST /api/security/invitations/{id}/approve`
**İzin:** `usermanagement:approve`
**Kim yapar:** Kurum yöneticisi (veya farklı bir yetkili)

Onaylanınca:
- Davet durumu `Approved` olur
- 7 günlük geçerlilik süresi (`ExpiresAt`) atanır
- Kayıt bağlantısı içeren **e-posta gönderilir**
- `InvitationApproved` eventi yayınlanır

Bağlantı formatı:
`https://app.mesnet.gov.tr/register?token={invitationId}`

> Bağlantıdaki `token` değeri davet kaydının UUID'sidir.

Reddedilirse:
- Durum `Rejected` olur, gerekçe kaydedilir
- `InvitationRejected` eventi yayınlanır, süreç sona erer

---

### 3. Kullanıcı Tamamlama (Kendi Kendine Kayıt)

**Endpoint:** `POST /api/security/invitations/{id}/complete`
**İzin:** Anonim (kullanıcı henüz kayıtlı değil)
**Kim yapar:** Davet edilen kişi

Gönderilen veriler:
- `invitationId` — E-postadaki token
- `username` — Seçilen kullanıcı adı
- `password` — Seçilen şifre

Handler sırasıyla şunları yapar:

1. **Daveti doğrula** — Mevcut, `Approved` ve süresi geçmemiş olmalı
2. **Keycloak'ta kullanıcı oluştur**
   - Ad, soyad, e-posta, kullanıcı adı, şifre yazılır
   - Şifre `Temporary = true` — ilk girişte değiştirilmesi zorunludur
3. **Keycloak'ta rol ata** — `targetRole` realm rolü olarak atanır
4. **Keycloak'ta attribute yaz**
   - `institution_id` → JWT token'a eklenir (protocol mapper gerekli)
   - `business_id` → İşletme kapsamlı roller için
5. **Yerel `UserAccount` kaydı oluştur** (Marten)
   - `KeycloakUserId` ile Keycloak kaydına bağlanır
   - `InstitutionId`, `BusinessId`, `Roles` tutulur
6. **Daveti `Completed` olarak işaretle**
7. **Event yayınla:**
   - `InvitationCompleted` — Davet iş akışı için
   - `UserCreated` — Diğer modüllerin dinlediği genel event

---

### 4. Daveti Yeniden Gönderme (Opsiyonel)

**Endpoint:** `POST /api/security/invitations/{id}/resend`
**Kim yapar:** Kurum yöneticisi

- Davet `Approved` durumunda olmalı
- Süresi dolduysa `ExpiresAt` 7 gün uzatılır
- E-posta tekrar gönderilir

---

## Veri Modeli

### `UserInvitation` (Marten — `security` schema)

| Alan | Açıklama |
|------|----------|
| `Id` | Davet ID'si — aynı zamanda token |
| `Email` | Davet edilen e-posta |
| `FirstName`, `LastName` | Ad soyad |
| `TargetRole` | Atanacak rol |
| `InstitutionId` | Kuruma bağlı davetler için |
| `BusinessId` | İşletmeye bağlı davetler için |
| `Status` | `PendingApproval` → `Approved` → `Completed` / `Rejected` / `Expired` |
| `ExpiresAt` | Onaydan itibaren 7 gün |
| `CreatedUserAccountId` | Tamamlandıktan sonra oluşan `UserAccount` ID'si |
| `Metadata` | Role özgü ek bilgiler (Dictionary) |

### `UserAccount` (Marten — `security` schema)

| Alan | Açıklama |
|------|----------|
| `Id` | Yerel kullanıcı ID |
| `KeycloakUserId` | Keycloak'taki kullanıcı UUID'si |
| `Username`, `Email` | Giriş bilgileri |
| `Roles` | Atanan roller listesi |
| `InstitutionId` | Kurum kapsamı |
| `BusinessId` | İşletme kapsamı (varsa) |
| `DirectPermissions` | Rol dışı özel izinler |

---

## Keycloak Entegrasyonu

### `institution_id` Claim'i

Davet tamamlandığında `CompleteInvitationHandler` Keycloak Admin API'ye yazarak kullanıcıya `institution_id` attribute'unu ekler. Bu attribute JWT token'a **protocol mapper** aracılığıyla aktarılır.

Keycloak realm yapılandırması (`mesnet-realm.json`) şu mapper'ı içermelidir:

```json
{
  "name": "institution_id",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-usermodel-attribute-mapper",
  "config": {
    "user.attribute": "institution_id",
    "claim.name": "institution_id",
    "jsonType.label": "String",
    "access.token.claim": "true"
  }
}
```

Frontend (`auth.ts`) bu claim'i `parsed.institution_id` ile okur:

```typescript
institutionId: parsed.institution_id ?? null
```

### Geliştirme / Seeder Ortamı

`MESNET.Seeder` ilk çalıştığında kurumu API aracılığıyla oluşturur ve ardından Keycloak Admin API'yi kullanarak `mesnet` realm'daki tüm mevcut kullanıcıların `institution_id` attribute'unu günceller. Bu sayede geliştirme ortamında el ile işlem gerekmez.

---

## Durum Geçişleri

```
              ┌──────────────────┐
  Oluştur ──▶ │  PendingApproval │
              └────────┬─────────┘
                       │
              ┌────────▼─────────┐      ┌──────────┐
              │     Onayla?      │──Hay──▶ Rejected │
              └────────┬─────────┘      └──────────┘
                    Evet│
              ┌─────────▼────────┐
              │    Approved      │──7 gün geçtiyse──▶ Expired
              └─────────┬────────┘
                        │ Kullanıcı tamamlarsa
              ┌──────────▼───────┐
              │    Completed     │
              └──────────────────┘
```

---

## Yayınlanan Eventler

| Event | Ne Zaman | Dinleyenler |
|-------|----------|-------------|
| `InvitationCreated` | Davet oluşturulduğunda | — |
| `InvitationApproved` | Onaylandığında | — |
| `InvitationRejected` | Reddedildiğinde | — |
| `InvitationCompleted` | Kullanıcı kaydı tamamlandığında | Security modülü iç akışı |
| `UserCreated` | Kullanıcı kaydı tamamlandığında | Institution, Business, Enrollment modülleri |

`UserCreated` eventi diğer modüllerin kullanıcıyı tanımasını sağlar. Örneğin bir `Teacher` daveti tamamlandığında Enrollment modülü bu eventi dinleyerek öğretmen profilini oluşturabilir.
