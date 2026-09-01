# MESNET — Üretim Kurulumu

Temiz bir sunucuda çalışan bir MESNET kurulumu bırakır. Depo kökündeki `docker-compose.yml`
**geliştirme/deneme** içindir; üretim için bu dizin kullanılır.

```bash
cd deploy
cp .env.example .env && chmod 600 .env
$EDITOR .env
./install.sh --dry-run      # önce ön kontroller
./install.sh
```

## Ön koşullar

| Gereksinim | Not |
| --- | --- |
| `podman compose` ya da `docker compose` | Betik hangisinin kurulu olduğunu kendi tespit eder |
| `curl`, `jq`, `openssl` | Yoksa betik açılışta durur |
| 80 ve 443 portları serbest | Caddy sertifikayı HTTP-01 ile alır |
| **Üç DNS kaydı** | `<alan>`, `auth.<alan>`, `docs.<alan>` — üçü de bu sunucuya |
| GHCR erişimi | İmajlar **private**: `podman login ghcr.io` (PAT, `read:packages`) |

`auth.<alan>` kaydı en kritik olanıdır: eksikse TLS alınamaz, Keycloak public adresten yanıt
vermez ve **giriş akışı hiç tamamlanmaz**. Betik bunu ön kontrolde uyarı olarak bildirir.

## Kurulum ne yapar

| Faz | İş |
| --- | --- |
| 1 · ön kontrol | Araçlar, `.env` bütünlüğü, örnek değer kalmış mı, DNS |
| 2 · veri | PostgreSQL ayağa kalkar; Keycloak için ayrı `keycloak` şeması açılır |
| 3 · keycloak | Keycloak + Caddy kalkar, realm **yakınsanır**, client secret okunup `.env`'e yazılır |
| 4 · uygulama | Kalan servisler kalkar, API sağlıklı olana kadar beklenir |
| 5 · yönetici | İlk `SystemAdmin` hesabı açılır (parola terminalden sorulur) |

Her faz idempotenttir; yarıda kalan kurulum yeniden çalıştırılarak tamamlanır. Tek faz için
`--only <faz>`.

## Realm neden import edilmiyor

Keycloak realm **import tek seferliktir**: dosyaya sonradan eklenen rol, politika ya da client
ayakta duran bir kaba **hiç ulaşmaz**. Ölçüldü (#195): geliştirme realm'inde depoda 11 rol
tanımlıyken çalışan realm'de yalnız 6'sı vardı; eksik beşi farklı sürümlerde eklenip her
seferinde unutulmuştu — ve hiçbir birim testi bunu göremezdi, çünkü testler depodaki dosyayı
okur, çalışan realm'i değil.

Bu yüzden `install.sh` realm'i **Admin API ile yakınsar**: realm yoksa şablondan yaratır, varsa
eksik rolleri ekler, öznitelik politikasını ve `mesnet-web` adreslerini yazar. Her koşuda
tekrarlanabilir; var olan kullanıcıları ve verileri bozmaz.

Rol listesinin **tek kaynağı** `keycloak/mesnet-realm.production.json` şablonudur; betik ikinci
bir liste tutmaz. Şablon da testle koda kilitlidir (`ProductionRealmTemplateTests`): yeni bir rol
`MesnetRoles`'a eklenip şablona eklenmezse test kırmızıya döner.

## Sırlar

- **Şablonda hiçbir kimlik bilgisi yoktur** ve testle kilitlidir. Depo PUBLIC'tir; şablona
  düşen bir parola geri alınamaz.
- **`mesnet-api` client secret'ını Keycloak üretir**, betik okuyup `.env`'e yazar. Elle yazılan
  bir değer iki tarafı ayrıştırır ve belirtisi hata değil, **"kullanıcı listesi boş"** olur.
- `.env` git'e eklenmez (kök `.gitignore`). İzni `600` olmalıdır; betik değilse uyarır.
- Betik hiçbir parolayı ekrana yazmaz ve hiçbirini kendi içinde saklamaz.

## Kurulumdan sonra — üç adım daha

Sistem ayağa kalkar ama **henüz kullanılabilir değildir**. Betik bu adımları sonunda birebir
çağrılabilir biçimde yazar:

1. **`POST /api/security/users/sync`** — `UserAccount` kaydı otoriterdir; kayıt yoksa token'daki
   roller izin **üretmez** ve uçlar 403 döner. İzin önbelleği nedeniyle etkisi **5 dakikaya
   kadar** gecikebilir; "işe yaramadı" sanıp geri almayın.
2. İlk okulu ve müdürünü arayüzden açın.
3. **`scripts/deploy-prereqs.sh`** — dağıtım ön koşulları. Atlanırsa sistem **hata vermez**,
   özellik sessizce çalışmaz. Ayrıntı ve sıra gerekçeleri:
   [`dagitim-on-kosullari.md`](../src/Docs/docs/infrastructure/dagitim-on-kosullari.md)

## Yükseltme

```bash
cd deploy
$EDITOR .env                 # MESNET_VERSION=<yeni etiket>
./install.sh --only keycloak # realm'i yakınsa (yeni rol/politika gelmiş olabilir)
./install.sh --only uygulama # imajları çek, servisleri değiştir
```

Sonra yeni sürümün dağıtım ön koşullarını koşturun. Sürüm notları hangi adımın gerektiğini yazar.

> [!CAUTION]
> **Conjoined kiracılık göçünden sonra eski imaja DÖNÜLMEZ.** Göç edilmiş bir veritabanına
> kiracılık öncesi kod bağlanırsa Marten şemayı kendi beklentisine uydurur ve kiracı damgasını
> **sessizce siler** — ölçüldü, üç GET isteği yetti. Hata yoktur, log temizdir, uçlar 200 döner.
> Tek çözüm yedekten dönmektir. Ayrıntı: `dagitim-on-kosullari.md`.

## İsteğe bağlı: Keycloak giriş teması

Üretim şablonunda `loginTheme` **yoktur**: tema dosyaları imaja gömülü değildir ve ayarı
bırakmak Keycloak'ı sessizce varsayılana düşürür. Kurum temasını istiyorsanız `compose.yml`
içindeki `keycloak` servisine mount ekleyin ve realm ayarını Admin konsolundan `mesnet` yapın:

```yaml
    volumes:
      - ../src/MESNET.AppHost/keycloak/themes/mesnet:/opt/keycloak/themes/mesnet:ro
```

## Sorun giderme

| Belirti | Bakılacak yer |
| --- | --- |
| API açılmıyor, log "Üretim yapılandırması eksik" | `.env` boş bırakılmış anahtar; mesaj hangisi olduğunu yazar |
| Her istek 401 | `Keycloak__auth-server-url` **public** adres olmalı; iç `keycloak:8080` yazılırsa issuer doğrulaması düşer |
| Giriş sonrası "Invalid redirect_uri" | `install.sh --only keycloak` — `mesnet-web` adresleri alan adıyla yazılır |
| Uçlar 403, roller doğru görünüyor | `POST /api/security/users/sync`, sonra **5 dakika** bekleyin |
| Kullanıcı listesi boş, hata yok | Client secret ayrışmış — `install.sh --only keycloak` |
| Caddy açılmıyor, `acme_ca: wrong argument count` | `.env` içindeki `ACME_CA` **tanımsız** değil **boş**; satırı silin ya da tam URL yazın |
| Listeler boş, hata yok | Dağıtım ön koşulları koşturulmamış olabilir — `deploy-prereqs.sh --dry-run` |
