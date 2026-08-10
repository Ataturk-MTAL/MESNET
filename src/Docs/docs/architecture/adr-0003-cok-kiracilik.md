---
title: "ADR-0003: Çok kiracılık — kiracı okuldur, izolasyon satır bazlıdır"
sidebar_label: "ADR-0003: Çok kiracılık"
---

# ADR-0003: Çok kiracılık — kiracı okuldur, izolasyon satır bazlıdır

**Durum:** Kabul edildi · **İlgili:** #147, #148, #149, #150, #151 · **Önceki:** [ADR-0001](./adr-0001-yetkilendirme-permission-bazli.md)

## Bağlam

Uygulama birden çok okul tarafından kullanılacak. Bugün tek okul çalışıyor ve **Marten'da
kiracılık yok**: izolasyon tamamen uygulama disiplini — `institution_id` claim'i ve elle yazılmış
`.Where(x => x.InstitutionId == ...)` filtreleri. `src/Modules/` altında ~326 dosya
`InstitutionId` referansı taşıyor.

Bu disiplinin kaçtığı bir yer bilinen kanıttır: `ResyncStaffBranchCodesHandler` tüm kurumları
filtresiz tarıyor, yalnız izinle korunuyor (#131).

### Sahibinin koyduğu kısıtlar

1. **"Sadece kendi verileri birbirinden izole olsa yeter."** İzolasyon sınırı okuldur.
2. **"Tüm okullar tüm işletmeleri listeleyebilir."** İşletme kataloğu **paylaşımlıdır**; bir
   işletme birden çok okuldan öğrenci alır.
3. **"Her okul aynı domain adresinden girecek — domain bazlı ayrışma olmayacak."** Tek kurulum,
   tek URL, tek giriş ekranı.
4. İl/ilçe Millî Eğitim Müdürlükleri ve Bakanlık altlarındaki okulları **raporlama amaçlı**
   okuyabilmeli; yazamaz.

## Karar

### 1. Kiracı = OKUL. Tek kiracılık ekseni.

İl, ilçe ve Bakanlık **kiracı değildir**. Aynı ildeki iki okul birbirinden izole olmalı; il
kiracı olsaydı olamazlardı. Hiyerarşinin üst basamakları ayrı kiracılar değil, bir kullanıcının
**okuma kapsamının genişliğidir** — Marten'ın `TenantIsOneOf(...)` / `AnyTenant()` yetenekleriyle,
**izinli ve tek bir açık tesisten** geçerek. Yazma yukarı doğru genişlemez.

### 2. İzolasyon satır bazlıdır (Marten conjoined)

Diğer seçenekler değerlendirildi ve elendi:

| Seçenek | Neden elendi |
| --- | --- |
| Okul başına ayrı kurulum | Kısıt 3 (tek domain, tek kurulum) doğrudan eliyor |
| Okul başına ayrı veritabanı | Paylaşımlı katalog cross-DB sorgu ister; il düzeyi okuma fan-out olur; tek kişilik ekip için N veritabanında migration + daemon + dayanıklı kuyruk yönetimi orantısız |
| Şema başına kiracı | **Marten desteklemiyor.** Ayrıca şema ekseni zaten *modül izolasyonuna* harcanmış |

Kalan tek makul seçenek conjoined. Kritik gözlem: **bugünkü sistem zaten conjoined kiracılık —
elle yazılmışı.** Marten'a geçiş veri modelini değiştirmez, **ihlal modunu** değiştirir: filtre
unutulunca "her şeyi görürsün" yerine "hiçbir şey görmezsin".

### 3. Kiracı anahtarı Keycloak'ta yaşamaz

**Otorite `UserAccount.InstitutionId`'dir** (Marten). Keycloak yalnız *kimlik doğrular*.
[ADR-0001](./adr-0001-yetkilendirme-permission-bazli.md)'in `branch_codes` için koyduğu kural
kiracı anahtarı için de geçerlidir ve gerekçesi ölçülmüştür:

- Keycloak'a kısmi PUT gövdesi, gövdede geçmeyen profil alanlarını **siler** ve yine **204**
  döner — sessiz veri kaybı (#190)
- Realm import **tek seferliktir**: depoya sonradan eklenen rol/politika mevcut kaba hiç
  ulaşmaz. Ölçüldü: depoda 11 realm rolü, çalışan realm'de 6 (#195)

**Token'ın imzalı olması, içeriğin bozulmadığı anlamına gelmez.**

### 4. Toptan `AllDocumentsAreMultiTenanted()` KULLANILMAZ

Kullanılsaydı ulusal alan/dal kataloğu, ulusal ücret parametreleri, kimlik katmanı ve
**paylaşımlı işletme kataloğu** da kiracı damgası alırdı. Damga bir kez atıldıktan sonra geri
almak veri göçü demektir.

Sınıflandırma kodda tutulur ve testle kilitlidir:
`src/MESNET.Common.Shared/Tenancy/DocumentTenancyMap.cs`. Sınıflandırılmamış belge bırakılamaz —
build kırılır.

| Sınıf | Adet | Anlamı |
| --- | ---: | --- |
| `Tenant` | 39 | `InstitutionId` taşır, kiracı sınırı içinde |
| `Shared` | 11 | Bilinçli olarak dışarıda — ulusal katalog/parametre, paylaşımlı işletme kataloğu |
| `Identity` | 2 | Kiracının kendisi ve kimlik katmanı |
| `MissingKey` | 4 | **Boşluk** — kiracıya ait veri taşıyor, kiracı anahtarı yok |

`MissingKey` sınıfı geçişten **önce boşalmalıdır**: `StudentNameView`, `StudentPaymentProfile`,
`StudentAbsenceView`, `AttendanceView`. Dördü de türetilmiştir (olaydan yeniden kurulur), yani
göç engeli değil **sızıntı yüzeyidir** — sorgu iki okulun satırını ayırt edemez.

## Zamanlama: geçiş tek okulluyken yapılır

Sezgiye aykırı ama gerekçe sağlam. Tek kiracılı dönem geçişin hem **en ucuz** hem **en güvenli**
penceresidir:

- **Ucuz:** mevcut satırlar tek `UPDATE` ile tek kiracıya damgalanır.
- **Güvenli:** tenant akışında bir hata yapılırsa (mesaj tenant'sız yayınlanır, tüketici yanlış
  session açar) **karışacak ikinci bölme yoktur**. Hata loglarla yakalanır, veri bozulmaz.

Aynı geçişi iki okul canlıyken yapmak, ilk hatanın **veri sızıntısı** olması demektir. Yani
"geçişi ikinci okula ertelemek" aslında onu en riskli ana ertelemektir.

## Doğrulanmış teknik gerçekler

Aşağıdakiler Marten ve Wolverine kaynaklarından teyit edildi — plan bunlara dayanıyor.

**Retrofit destekleniyor.** `TenancyStyle.Conjoined`'e geçildiğinde Marten mevcut tablolara
`tenant_id` kolonunu ekler ve mevcut satırlar `*DEFAULT*` değerini alır (`Bug_3145_migration_to_tenanted`).
Olay tablolarında benzersizlik `(tenant_id, stream_id, version)` olur — tek kiracıda değişiklik
yaratmaz.

**Elle SQL göçünün kapsamı:** `mt_streams` + `mt_events` + **kiracılı her belge tablosu**
(inline snapshot'lar ve projeksiyon belgeleri dâhil). `mt_event_progression` kiracı bilgisi
tutmaz, yani async daemon ilerlemesi bozulmaz.

**Varsayılan kiracı kapatılabilir:** `StoreOptions.Advanced.DefaultTenantUsageEnabled = false` →
tenant'sız session `DefaultTenantUsageDisabledException` fırlatır. **Shared belgeler kiracılı
session'dan filtresiz görünmeye devam eder** — paylaşımlı katalog bu ayarla çatışmaz.

**Wolverine kiracıyı taşır:** cascading mesajlar kiracıyı devralır; handler içinden elle
`PublishAsync`/`SendAsync` çağrıları da `MessageContext.TenantId` üzerinden otomatik damgalanır.
**Ama kiracısız gelen bir mesaj sessizce varsayılan kiracıya düşer** — yerleşik gürültülü hata
yoktur.

:::tip Son iki madde birlikte okunmalı
Sessiz düşüş, `DefaultTenantUsageEnabled = false` ile **exception'a** çevrilir. Planın en riskli
adımının başlıca azaltımı budur.
:::

## Plan

Sıralama ilkesi: **geri alınabilir işler önce, tek yönlü kapı en sonda ve en çok bilgiyle.**
1–4 arası adımlar conjoined hiç açılmasa bile bugün değerlidir.

| # | İş | Risk | Geri alınabilir | Bitti ölçütü |
| ---: | --- | --- | --- | --- |
| 0 | Belge sınıflandırması + drift kilidi | — | — | ✅ Tamamlandı (#201) |
| 1 | Dört `MissingKey` görünüme `InstitutionId` + backfill | Düşük | Evet | `MissingKey` sınıfı boş; teste "yeni tip eklenemez" mührü |
| 2 | Kiracı otoritesinin kalan boşlukları | Orta | Evet, iki PR | ✅ Tamamlandı (#223 + bu PR) |
| 3 | Keycloak sertleştirme: realm drift denetimi + user PUT'ları GET+merge'den geçsin | Düşük | Evet | Drift denetimi CI/smoke'ta yeşil; merge davranışı testli |
| 4 | `Business.InstitutionId` → provenance (`RegisteredByInstitutionId`) | Orta, davranış farkı **sıfır** | Evet | ✅ Tamamlandı |
| 5 | **Marten conjoined açılışı** | Yüksek | **TEK YÖNLÜ KAPI** | İzolasyon test paketi yeşil; `tenant_id = *DEFAULT*` satır sayısı sıfır |
| 6 | İkinci okul kontrol listesi | — | — | İzolasyon smoke'u iki gerçek okulla geçti |

### Adım 2'nin iç sırası (bozulmamalı) — tamamlandı

1. ✅ Mevcut kullanıcılar için `UserAccount.InstitutionId` **backfill** (personel kaydından) — #223
2. ✅ `EnrichInstitutionClaimAsync`'teki token-kabul yolunu kapat
3. ✅ `SyncUsersFromKeycloak`'ın otoriter kaydı ezmesini durdur
4. ✅ `ChangeUserInstitution` ucu + cache invalidation
5. ✅ `CreateUser`'ın (ve davet kabulünün) Keycloak'a `institution_id` yazmasını bırak

**Backfill'siz 2. madde mevcut kullanıcıları kilitler.** Bu yüzden 1. madde ayrı deploy oldu.

#### Sonuçta ne değişti

**Kurum kapsamının kaynağı ikiye indi ve ikisi de sunucu tarafında:** kullanıcı kaydı
(`UserAccount.InstitutionId`, otorite) ve personel kaydı yedeği (`staff[]` eşleşmesi, geçiş
adımı). Token'daki `institution_id` **hiçbir koşulda** okunmuyor — kayıt boş olsa bile. Kapsamsız
kalmak, kullanıcının kendi seçtiği kiracıya düşmekten iyidir.

**Kiracı anahtarının tek yazma yolu** `POST /api/security/users/{id}/institution`. Kapsam kararı
`UserInstitutionScopePolicy`'de: aktör yalnız **kendi kurumuna** bağlayabilir, başka kuruma bağlı
kullanıcıyı devralamaz, bağı çözebilir. Faz 1'de tek kurum olduğu için kural bugün hep sağlanıyor;
kontrol adım 5'ten sonra anlam kazanacağı için şimdiden yazıldı.

**`branch_codes`'tan neden daha katı:** alan kapsamı kiracı *içinde* bir yetki sınırıdır ve orada
kayıt boşken token yedeği hâlâ kabul edilir (#126). `institution_id` ise kiracı anahtarının
kendisidir; orada "yedek kaynak" diye bir şey olamaz.

**Yeni işletim gerçeği:** `SyncUsersFromKeycloak` artık kurum bağı kurmuyor. Dışarıdan gelen
kullanıcı **kapsamsız doğuyor** ve bağı idari bir işlem kuruyor. Sync sonucu bunu sayıyor
(`WithoutInstitution`) ve uç mesajı söylüyor — sessiz kalırsa "sync çalıştı" sanılırdı.

**Kalan borç:** `business_id` hâlâ token claim'i olarak okunuyor ve `CreateUser` onu Keycloak'a
yazıyor. Kiracı anahtarı değil, kiracı *içinde* bir kapsam olduğu için bu adımın konusu değildi;
ama aynı unmanaged-öznitelik riskini taşıyor ve ayrı ele alınmalı.

### Adım 4 — ne değişti, ne değişmedi

**Değişen: ad ve dolayısıyla okuma.** `Business.InstitutionId` → `RegisteredByInstitutionId`;
`BusinessRegistered/Approved/Activated` olaylarında da aynı. Alan artık adıyla söylüyor:
**kaydı hangi okul girdi**. Eski ad "bu işletme şu okula ait" diye okunuyordu ve adım 5'te bu
okuma paylaşımlı kataloğu kiracıya bölerdi — bir okul diğerinin işletmesini hiç göremezdi.

**Değişmeyen: davranış.** Hiçbir sorgu bu alanla filtrelemiyordu, bugün de filtrelemiyor.
Faz 1'de tek kurum var, yani tüm değerler zaten aynı.

**Bilinçli olarak bırakılan yaklaşım:** `BusinessCoordinationView.InstitutionId` hâlâ
provenance'tan besleniyor — yani işletmeyi ilk kaydeden okul, onu koordine eden okul sayılıyor.
Çok okullu yapıda yanlış olur: aynı işletmeye ikinci okuldan öğrenci yerleştirildiğinde o okul
işletmeyi koordinasyon ekranlarında göremez. Doğrusu kapsamı **ilişkiden** (yerleştirme)
türetmektir; bu, aynı vergi numaralı kayıtların birleştirilmesiyle birlikte ayrı bir domain
migration'dır (bkz. Kapsam dışı). Çevirim tek bir yerde toplandı — `BusinessScopeOrigin` —
böylece o migration geldiğinde değişecek tek nokta belli.

**Kilit:** `BusinessProvenanceDriftTests` alanın okuyucularını listeyle sınırlar. Yeni bir
okuyucu **bir karardır**, kazara eklenen satır değil: test kırmızıya döner, kararı veren listeye
gerekçesiyle ekler. İkinci test eski adın entity'ye geri gelmesini engeller.

**Göç gerekir:** Marten belgeyi JSON tuttuğu için ad değişikliği anahtarı değiştirir. Atlanırsa
sorgular etkilenmez ama işletme onaylandığında koordinasyon görünümü `Guid.Empty` kapsamıyla
açılır ve işletme ekranlardan kaybolur. Boş provenance `LogWarning` üretir; SQL
`dagitim-on-kosullari.md`'de.

### Adım 5, birinci yarı — kiracı hattı (kapı hâlâ kapalı)

Conjoined açılmadan önce kiracının **taşınabilir** olması gerekiyor. Bu yarı geri alınabilir ve
davranışı değiştirmez; ölçülen üç gerçek:

**Göç yıkıcı DEĞİL.** Yama üretilip okundu (issue #149 adım 3). Her kiracılı tablo için üç
ifade: `add column tenant_id ... DEFAULT '*DEFAULT*'`, eski PK'yı düşür, `PRIMARY KEY
(tenant_id, id)` ekle. `DROP TABLE` / `DELETE` / `TRUNCATE` **yok**; 45 tablo, 144 delta.
`mt_streams`/`mt_events` zaten `tenant_id` taşıyor — değişen yalnız PK, FK ve benzersiz indeks.
47 `DROP CONSTRAINT ... CASCADE` var ama veritabanında tek FK bulunuyor (`fkey_mt_events_stream_id`)
ve yama onu `(tenant_id, stream_id)` olarak yeniden kuruyor.

**Wolverine düz Minimal API'de kiracıyı otomatik tespit etmez** — otomatik tespit yalnız
`Wolverine.Http` uçları içindir. Ama `IMessageBus` *scoped* ve `TenantId` yazılabilir; tek bir
middleware onu koyunca 219 `bus.InvokeAsync` çağrısının hiçbirine dokunmak gerekmiyor,
cascading mesajlar ve `PublishAsync` kiracıyı devralıyor.

**Kiracı uydurulmaz.** Kapsamsız kullanıcıyı varsayılan ya da `platform` kiracısına düşürmek,
yazmalarını sessizce yanlış bölmeye göndermek olurdu. Çözülemezse kiracı konmaz; kapı
açıldığında erişim gürültülü biçimde başarısız olur. Karar `TenantResolution` içinde ve testli:

| Kullanıcı | Kiracı |
| --- | --- |
| Kurumu olan | kendi kurumunun kimliği |
| Kurumu yok, `platform:` izni var (`SystemAdmin`) | `platform` |
| Kurumu yok, platform izni de yok | **yok** |

Kurumu olan kullanıcı platform izni de taşısa **kendi okulunda kalır** — aksi hâlde okul müdürü
ulusal katmana yazarken okul verisinden kopardı.

`platform` bir okul değildir ve kiracıya ait belge yazmaz. Var olma sebebi kiracısız session'ın
adım 5'te yasaklanacak olması: kapsam dışı işlerin (ulusal parametre, alan/dal kataloğu, kimlik
katmanı) de **adı olan** bir kimlik altında çalışması gerekir, `*DEFAULT*` gibi kimin olduğu
belirsiz bir kova değil.

**Sınıflandırma artık çalışma zamanında da denetleniyor.** Kaynak taraması
(`DocumentTenancyDriftTests`) `Schema.For<T>()` bildirimlerini metin olarak arar; Marten ise
bildirilmemiş bir tipi `session.Store(x)` anında kendiliğinden kaydeder ve öyle bir tip iki
kontrolün arasından geçerdi. `DocumentTenancyVerificationHostedService` açılışta Marten'ın
**gerçekten tanıdığı** tipleri haritayla karşılaştırır: Development'ta sapma açılışı durdurur.

### Adım 5'in en riskli yeri ve azaltımları

Tehlike modu: mesaj tenant'sız yayınlanır → tüketici varsayılan kiracı session'ı açar → yazma
**sessizce yanlış bölmeye** gider. HTTP 200, log temiz.

1. **Tek kiracılıyken aç** — karışacak bölme yok; prod bedava staging olur
2. **`DefaultTenantUsageEnabled = false`** — sessiz yanlış-yazma exception'a döner
3. **`tenant_id = *DEFAULT*` satır sayısı sürekli sıfır** — basit SQL smoke
4. **İki sahte kiracıyla izolasyon paketi** CI'da kalıcı — tek seferlik geçiş testi değil
5. **Kuyruk boşken deploy** — geçiş anında bekleyen tenant'sız zarflar en sinsi durum

## Sonuçlar

**Kazanç:** izolasyon disiplinden altyapıya taşınır; filtre unutmak veri sızdırmak yerine boş
sonuç üretir. Yeni belge eklemek kiracı kararını zorunlu kılar.

**Bedel:** yazma hattının tamamı kiracı taşımak zorunda; test ve seeder kiracı kurulumu ister;
`AnyTenant`/`TenantIsOneOf` yeni bir bypass yüzeyi açar ve ADR-0001 disiplininde ayrı izinle
korunmalıdır.

**Açık kalan:** conjoined'e geçtikten sonra async projeksiyonların **tam yeniden kurulum**
gerektirip gerektirmediği. Kaynaklar "genelde önerilir" diyor, kesin kural vermiyor — adım 5'te
ölçülecek, dokümana güvenilmeyecek.

## Kapsam dışı

- **`Business.InstitutionId`'nin kaldırılması bir index işi değil, ayrı bir domain
  migration'dır** (#150 index işini kapsar): aynı vergi numaralı kayıtların birleştirilmesi ve
  okula bağlı alanların ilişki entity'sine taşınması.
- Çok okullu işletme durum yönetimi (#151) — "farklı kurumlardan iki bildirimle işletme küresel
  kapanır" kuralı teknik değil kurumsal-politiktir; ikinci okul gerçekleşmeden tasarlanması
  spekülasyondur.
