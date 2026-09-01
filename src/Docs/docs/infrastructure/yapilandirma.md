---
title: Yapılandırma ve Ortam Değişkenleri
sidebar_label: Yapılandırma
---

# Yapılandırma ve Ortam Değişkenleri

MESNET'in yapılandırması **dört ayrı yüzeydedir** ve hangisinin nerede yaşadığını bilmek,
"neden çalışmıyor" sorularının çoğunu baştan keser.

| Yüzey | Dosya | Git'te? | Ne için |
| --- | --- | --- | --- |
| Dağıtım (compose) | `.env` | **Hayır** — `.gitignore`'lu | `docker-compose.yml`'ın okuduğu her şey: alan adı, TLS, parolalar |
| Dağıtım örneği | `.env.example` | Evet | Yukarıdakinin placeholder'lı şablonu |
| Geliştirme (backend) | `appsettings.*.json` | **Hayır** | `.sample.json`'dan kopyalanır |
| Geliştirme (frontend) | `src/WebUI/.env.development` | Evet | Yalnız yerel URL'ler — gizli değer yok |

:::danger Gerçek parola git'e girmez
`.env` ve `appsettings.*.json` **izlenmiyor**. Örnek dosyalar yalnız placeholder taşır
(`GUCLU_SIFRE_BURAYA`). Bir örnek dosyaya gerçek değer yazmak, onu git geçmişine kalıcı olarak
gömmek demektir — geçmişten silmek yeniden yazma (rewrite) gerektirir ve deponun herkeste
bozulmasına yol açar.
:::

## `.env` — dağıtım değişkenleri

`.env.example`'ı kopyalayıp doldurun:

```bash
cp .env.example .env
```

`.env.example` ile `docker-compose.yml` arasında **tam eşleşme** aranır: compose'un okuduğu her
değişkenin örnekte bir karşılığı vardır. Yeni bir değişken eklerken **ikisini birden**
güncelleyin — örnekte olmayan bir değişken, dağıtımda boş string olarak gelir ve çoğu zaman
hata değil **sessiz yanlış davranış** üretir.

### Alan adı ve TLS

| Değişken | Anlamı |
| --- | --- |
| `APP_DOMAIN` | Ana alan adı. `docs.<APP_DOMAIN>` ve `auth.<APP_DOMAIN>` alt alanları buradan türer |
| `ACME_EMAIL` | Let's Encrypt sertifika bildirimleri |
| `ACME_CA` | ACME sağlayıcısı. Boş bırakılabilir — compose üretim Let's Encrypt'i verir |

Caddy üç alan adı için otomatik HTTPS alır: `APP_DOMAIN`, `docs.APP_DOMAIN`, `auth.APP_DOMAIN`.
Üçünün de **genel DNS'te çözülmesi** ve `:80`/`:443` portlarının dışarıdan erişilebilir olması
gerekir — ACME doğrulaması bunu ister.

### Servis kimlik bilgileri

| Değişken | Not |
| --- | --- |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` | Veritabanı |
| `RABBITMQ_USER` / `RABBITMQ_PASSWORD` | Modüller arası mesajlaşma |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | Belge/dekont depolama |
| `KEYCLOAK_ADMIN_USER` / `KEYCLOAK_ADMIN_PASSWORD` | Keycloak yönetim konsolu |
| `KEYCLOAK_CLIENT_SECRET` | API'nin Keycloak istemci sırrı |
| `KEYCLOAK_URL` / `KEYCLOAK_HOSTNAME` | Keycloak'ın **dışarıdan görünen** adresi |
| `FRONTEND_URL` | Backend'in e-posta/yönlendirme bağlantılarında kullandığı adres |
| `SMTP_HOST` / `SMTP_FROM_EMAIL` | Bildirim e-postaları |

### Frontend derleme değişkenleri (`VITE_*`)

| Değişken | Not |
| --- | --- |
| `VITE_KEYCLOAK_REALM` | Varsayılan `mesnet` |
| `VITE_KEYCLOAK_CLIENT_ID` | Varsayılan `mesnet-web` |
| `VITE_OSRM_URL` | Opsiyonel rota servisi; boş bırakılabilir |

:::warning `VITE_*` değerleri BUILD ZAMANINDA gömülür
Bunlar çalışma anında okunmaz — Vite onları SPA paketinin **içine yazar**
(`src/caddy/Dockerfile`, compose'daki `build.args`). `APP_DOMAIN` ya da herhangi bir `VITE_*`
değiştiğinde **imajı yeniden derlemek** gerekir; konteyneri yeniden başlatmak yetmez.

Belirti: alan adını değiştirdiniz, API çalışıyor ama tarayıcı hâlâ eski Keycloak adresine
gidiyor ve giriş döngüye giriyor.

`VITE_*` değerleri **gizli değildir** — paketin içinde herkese açıktır. Oraya sır koymayın.
:::

## Test (staging) dağıtımı

Test ortamı üretimle **aynı 20 değişkeni** kullanır, farklı değerlerle. Ama dört nokta ayrıca
dikkat ister; hepsi ölçülerek belirlendi.

### 1. Sertifika sağlayıcısını staging'e çevirin

```dotenv
ACME_CA=https://acme-staging-v02.api.letsencrypt.org/directory
```

Üretim Let's Encrypt'in **aynı alan adı kümesi için haftada 5 sertifika** limiti vardır. Test
ortamı sık sık yeniden kurulur; limit dolduğunda ortam **HTTPS'siz kalır** ve bekleme süresi
gün mertebesindedir. Staging sertifikaları tarayıcıda "güvenilmez" görünür — bu beklenen
davranıştır, hata değil.

:::danger Caddy'ye BOŞ `ACME_CA` geçmeyin
Ölçüldü: boş değer `acme_ca` direktifini argümansız bırakır ve **Caddy hiç açılmaz**:

```
Error: parsing caddyfile tokens for 'acme_ca': wrong argument count or unexpected line ending
```

Caddyfile'ın `{$VAR:varsayılan}` sözdizimi yalnız **tanımsız** değişkende devreye girer, boş
string'te **devreye girmez**. Bu yüzden varsayılan `docker-compose.yml`'da da verilir
(`${ACME_CA:-https://acme-v02…}`) — `.env`'de satır hiç olmasa da, `ACME_CA=` diye boş
bırakılsa da üretim Let's Encrypt gelir. Compose'u atlayıp Caddy'yi doğrudan koşturuyorsanız
değişkeni ya **hiç tanımlamayın** ya da tam bir URL verin.
:::

:::note `caddy-data` birimini silmeyin
Alınan sertifikalar `caddy-data` biriminde durur. Birimi silmek yeniden başvuru demektir ve
üretim CA'sında doğrudan limite yazılır. `podman compose down` birimi silmez; `down -v` siler.
:::

### 2. Sırları üretimden AYIRIN

Test ortamına üretim parolalarını kopyalamak, test ortamının erişim yüzeyini üretimin yüzeyi
yapar. Test ortamları tanımı gereği daha gevşek korunur.

### 3. Keycloak realm'i tek seferlik import edilir

`mesnet-realm.json` **yalnız ilk açılışta** okunur. Depoya sonradan eklenen rol, politika ya da
client ayarı **mevcut bir kaba hiç ulaşmaz** — ve bunu hiçbir birim testi göremez, çünkü testler
depodaki dosyayı okur, çalışan realm'i değil.

Açılışta `RealmVerificationHostedService` çalışan realm'i depodaki tanımla karşılaştırır ve
sapmayı bildirir. Ayrıntı: [Keycloak](keycloak.md).

### 4. Dağıtım ön koşullarını koşturun

Bazı adımlar dağıtımdan sonra elle çalıştırılır. Atlanınca sistem **hata vermez** — özellik
sessizce çalışmaz.

```bash
./scripts/deploy-prereqs.sh --dry-run    # önce planı görün
./scripts/deploy-prereqs.sh
```

Ayrıntı ve sıra gerekçeleri: [Dağıtım Ön Koşulları](dagitim-on-kosullari.md).

## Geliştirme ortamı

Üç yapılandırma dosyası git'te izlenmiyor; örneklerinden kopyalanır:

```bash
cp src/MESNET.AppHost/appsettings.sample.json                   src/MESNET.AppHost/appsettings.json
cp src/MESNET.Presentation/appsettings.Development.sample.json  src/MESNET.Presentation/appsettings.Development.json
cp src/MESNET.Seeder/appsettings.sample.json                    src/MESNET.Seeder/appsettings.json
```

:::warning `git pull` bu dosyaları silebilir
Dosyalar takipten çıkarıldığı için, değişikliği içeren commit'i **ilk kez** çektiğinizde git
diskteki kopyanızı da siler. `git pull` sonrası uygulama "endpoint boş" ya da "credentials not
initialized" gibi hatalarla açılmıyorsa, önce bu üç dosyanın yerinde olduğunu kontrol edin.
:::

## Kara kutu API testlerinin ortam değişkenleri

`tests/MESNET.Api.Tests` **çalışan bir API'ye** karşı koşar ve yapılandırmasını ortam
değişkenlerinden alır. **Hepsinin geliştirme varsayılanı vardır**, yani yerelde hiçbir şey
ayarlamadan koşar.

| Değişken | Varsayılan | Okuyan |
| --- | --- | --- |
| `API_BASE_URL` | `http://localhost:5270` | `ApiTestFixture` |
| `KEYCLOAK_TOKEN_URL` | `http://localhost:8080/realms/mesnet/…/token` | `ApiTestFixture` |
| `KEYCLOAK_CLIENT_ID` | `mesnet-api` | `ApiTestFixture` |
| `KEYCLOAK_CLIENT_SECRET` | `dev-secret` | `ApiTestFixture` |
| `API_TEST_USERNAME` | `admin` | `ApiTestFixture` |
| `API_TEST_PASSWORD` | `admin` | `ApiTestFixture` |
| `ConnectionStrings__mesnet` | yerel geliştirme bağlantısı | `TenantStampIntegrityTests` |

CI yalnız ilk ikisini ayarlar (`scripts/ci-local.sh`, `.github/workflows/ci.yml`); kalanlar
varsayılanda kalır.

:::danger `KEYCLOAK_CLIENT_SECRET` adı ÇAKIŞIYOR
Aynı ad hem `.env`'de (dağıtımın gerçek istemci sırrı) hem test fixture'ında geçiyor. Dağıtım
`.env`'ini kabuğunuza `export` edip ardından API testlerini koşarsanız, testler **üretim
istemci sırrıyla** kimlik almaya çalışır. Test koşarken o kabuğu temiz tutun.
:::

Bunlar için ayrı bir `.env` dosyası **yoktur ve olmamalıdır**: .NET test host'u `.env`
dosyalarını kendiliğinden okumaz, dolayısıyla öyle bir dosya hiçbir şey yapmayan ama iş görüyor
görünen bir dosya olurdu. Değer geçmek gerekiyorsa değişkeni komut satırında verin:

```bash
API_BASE_URL=https://test.mesnet.example.com \
  dotnet test tests/MESNET.Api.Tests/MESNET.Api.Tests.csproj
```
