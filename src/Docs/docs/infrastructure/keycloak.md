---
title: Keycloak Kimlik Yönetimi
---

MESNET, kimlik doğrulama ve yetkilendirme için **Keycloak** kullanır.
Frontend (SPA) PKCE flow ile doğrudan Keycloak'a bağlanır; backend JWT doğrulaması yapar.

## Mimari Genel Bakış

```text
Frontend (Vue 3 SPA)
  └── PKCE Authorization Code Flow
        ↓
Keycloak (Realm: mesnet)
  ├── mesnet-web   → Public client (PKCE, no secret)
  └── mesnet-api   → Confidential client (service account)
        ↓
Backend (.NET API)
  └── JWT Bearer doğrulama + realm role kontrolü
```

---

## Realm Yapısı

### Client'lar

| Client | Tip | Kullanım |
| --- | --- | --- |
| `mesnet-web` | Public (PKCE) | Vue 3 SPA — kullanıcı login |
| `mesnet-api` | Confidential | Backend service account — Keycloak Admin API |

### Realm Roller

| Rol | Açıklama |
| --- | --- |
| `InstitutionManager` | Kurum yöneticisi (Müdür / Müdür Yardımcısı) |
| `InstitutionStaff` | Kurum personeli |
| `Teacher` | Koordinatör öğretmen |
| `Student` | Öğrenci |
| `DepartmentHead` | Alan şefi |
| `CompanyManager` | İşletme yetkilisi |

### Token Claim'leri

Hem `mesnet-web` hem `mesnet-api` client'larında aşağıdaki custom claim'ler tanımlıdır:

| Claim | User Attribute | Açıklama |
| --- | --- | --- |
| `realm_access.roles` | — | Realm rolleri (dizi) |
| `institution_id` | `institution_id` | Kullanıcının bağlı olduğu kurum |
| `business_id` | `business_id` | İşletme yetkilisi için işletme ID |
| `student_id` | `student_id` | Öğrenci kullanıcı için öğrenci ID |

---

## Türkçe Dil Desteği

Keycloak 26 yerleşik olarak Türkçe destekler. Realm JSON'da aşağıdaki ayarlar ile login ekranı varsayılan olarak Türkçe gelir:

```json title="mesnet-realm.json"
{
  "internationalizationEnabled": true,
  "supportedLocales": ["tr", "en"],
  "defaultLocale": "tr",
  "loginTheme": "mesnet"
}
```

| Ayar | Değer | Açıklama |
| --- | --- | --- |
| `internationalizationEnabled` | `true` | Çoklu dil desteğini aktifleştirir |
| `supportedLocales` | `["tr", "en"]` | Desteklenen diller |
| `defaultLocale` | `"tr"` | Varsayılan dil |
| `loginTheme` | `"mesnet"` | Özel login teması |

Kullanıcılar login ekranındaki dil seçiciden Türkçe↔İngilizce geçiş yapabilir.

---

## MESNET Login Teması

Keycloak'ın varsayılan PatternFly teması yerine, Quasar Framework ile uyumlu **Material Design** teması oluşturulmuştur.

### Dizin Yapısı

```text
src/MESNET.AppHost/keycloak/themes/mesnet/login/
  ├── theme.properties          ← Tema konfigürasyonu
  └── resources/
      └── css/
          └── mesnet.css        ← Material Design stil dosyası
```

### theme.properties

```properties title="theme.properties"
parent=keycloak
import=common/keycloak
styles=css/login.css css/mesnet.css
locales=tr,en
```

| Ayar | Açıklama |
| --- | --- |
| `parent=keycloak` | Varsayılan Keycloak temasını baz alır |
| `import=common/keycloak` | Ortak kaynakları (JS, resimler) dahil eder |
| `styles=css/login.css css/mesnet.css` | Önce varsayılan stil, sonra MESNET override |
| `locales=tr,en` | Tema dil dosyaları |

### Renk Paleti

Quasar Framework ile tutarlılık sağlamak için Material Design renk paleti kullanılır:

| Değişken | Değer | Kullanım |
| --- | --- | --- |
| `--mesnet-primary` | `#1976D2` | Material Blue 700 — butonlar, başlıklar, linkler |
| `--mesnet-primary-dark` | `#1565C0` | Hover durumu |
| `--mesnet-primary-light` | `#42A5F5` | Vurgular |
| `--mesnet-secondary` | `#26A69A` | Material Teal 400 |
| `--mesnet-surface` | `#FFFFFF` | Kart arka planı |
| `--mesnet-background` | `#F5F5F5` | Sayfa arka planı |
| `--mesnet-error` | `#C10015` | Quasar negative renk |

### Stil Özellikleri

- **Arka plan:** Mavi-yeşil gradient (`#E3F2FD → #BBDEFB → #E8F5E9`)
- **Login kartı:** Beyaz, yuvarlatılmış köşeler (`8px`), Material shadow
- **Butonlar:** Full-width, uppercase, Material raised stil, hover animasyonu
- **Form alanları:** Material outlined input stil, focus'ta mavi border + glow
- **Hata mesajları:** Material Design renk kodlaması (kırmızı/sarı/yeşil)
- **Dil seçici:** Material dropdown stil
- **Responsive:** 600px altında mobil uyumlu padding

---

## Aspire AppHost Konfigürasyonu

```csharp title="src/MESNET.AppHost/Program.cs"
var keycloak = builder.AddKeycloak("keycloak", port: 8080, adminPassword: keycloakPassword)
    .WithRealmImport("./keycloak")
    .WithBindMount("./keycloak/themes/mesnet", "/opt/keycloak/themes/mesnet")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
```

| Konfigürasyon | Açıklama |
| --- | --- |
| `WithRealmImport("./keycloak")` | İlk başlatmada realm JSON'u import eder |
| `WithBindMount(...)` | MESNET temasını container'a bağlar |
| `WithDataVolume()` | Keycloak verileri persistent volume'da saklanır |
| `ContainerLifetime.Persistent` | AppHost kapansa bile container ayakta kalır |

---

## Güvenlik Ayarları

Realm JSON'da brute-force koruma aktiftir:

| Ayar | Değer | Açıklama |
| --- | --- | --- |
| `bruteForceProtected` | `true` | Brute-force koruması aktif |
| `failureFactor` | `5` | 5 başarısız denemeden sonra kilitle |
| `waitIncrementSeconds` | `60` | Her kilitte 60 sn artan bekleme |
| `maxFailureWaitSeconds` | `900` | Maksimum 15 dk bekleme |
| `maxDeltaTimeSeconds` | `43200` | 12 saat sonra sayaç sıfırlanır |

### Token Ömürleri

| Token | Süre | Açıklama |
| --- | --- | --- |
| Access token | 5 dk | Kısa ömür — güvenlik için |
| SSO session idle | 30 dk | İnaktif oturum zaman aşımı |
| SSO session max | 8 saat | Maksimum oturum süresi |
| Offline session idle | 30 gün | Offline token idle |
| Offline session max | 60 gün | Offline token maksimum |

---

## Geliştirici Notları

### İlk Kurulum

Keycloak container'ı **persistent** çalışır. Realm import yalnızca **ilk oluşturmada** gerçekleşir.

:::tip Temayı test etmek için

Tema dosyaları bind mount ile bağlı olduğundan, CSS değişiklikleri **sayfa yenilemeyle** anında yansır. `theme.properties` değişiklikleri için Keycloak restart gerekir.

:::

:::caution Realm değişikliklerini uygulamak

`mesnet-realm.json` değişiklikleri mevcut container'a otomatik uygulanmaz. İki yol vardır:

1. **Container'ı yeniden oluştur** (önerilen — dev ortamı):
   ```bash
   podman stop keycloak && podman rm keycloak
   # Aspire AppHost yeniden başlatıldığında import çalışır
   ```

2. **Admin Console'dan manuel ayarla:**
   `http://localhost:8080/admin` → Realm Settings → Localization / Themes

:::

### Test Kullanıcıları

| Kullanıcı | Şifre | Rol |
| --- | --- | --- |
| `admin` | `admin` | InstitutionManager |
| `teacher1` | `teacher1` | Teacher |
| `student1` | `student1` | Student |

:::info Seeder Kullanıcıları

Yukarıdaki kullanıcılar realm JSON'da tanımlıdır. Seeder çalıştığında ek kullanıcılar
(öğretmenler, öğrenciler, işletme yetkilileri) Keycloak Admin API üzerinden oluşturulur.

:::
