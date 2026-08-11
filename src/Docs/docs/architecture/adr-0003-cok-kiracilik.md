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

- Keycloak'a `attributes` içeren bir PUT gövdesi, gövdede geçmeyen profil alanlarını
  (`firstName`, `email`) **siler** ve yine **204** döner — sessiz veri kaybı (#190).
  Kuralın tam ölçümü aşağıda, "Adım 3" bölümünde
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
| `Tenant` | 42 | `InstitutionId` taşır, kiracı sınırı içinde |
| `Shared` | 11 | Bilinçli olarak dışarıda — ulusal katalog/parametre, paylaşımlı işletme kataloğu |
| `Identity` | 3 | Kiracının kendisi, kullanıcı kaydı ve davet |
| `MissingKey` | 0 | **Boşaldı** (#147 adım 1) — kiracı verisi taşıyıp anahtarı olmayan belge kalmadı |

`MissingKey` sınıfı geçişten **önce boşaldı** (#147 adım 1): `StudentNameView`,
`StudentPaymentProfile`, `StudentAbsenceView`, `AttendanceView`. Dördü de türetilmiştir (olaydan
yeniden kurulur), yani göç engeli değil **sızıntı yüzeyiydi** — sorgu iki okulun satırını ayırt
edemezdi. Sınıfın boş kalması `IdentityLayerTenancyTests` ile kilitlidir.

`Identity` sınıfı adım 5'te ikiden üçe çıktı: `UserInvitation` oraya taşındı. Daveti tamamlayan
kişinin henüz kullanıcı kaydı, dolayısıyla kiracısı yoktur; daveti okumadan da kiracı bilinemez —
`UserAccount` ile aynı döngüsellik.

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
| 1 | Dört `MissingKey` görünüme `InstitutionId` + backfill | Düşük | Evet | ✅ Tamamlandı (#203) |
| 2 | Kiracı otoritesinin kalan boşlukları | Orta | Evet, iki PR | ✅ Tamamlandı (#223 + #224) |
| 3 | Keycloak sertleştirme: realm drift denetimi + user PUT semantiği | Düşük | Evet | ✅ Tamamlandı (#195 drift + yazma semantiği bu PR) |
| 4 | `Business.InstitutionId` → provenance (`RegisteredByInstitutionId`) | Orta, davranış farkı **sıfır** | Evet | ✅ Tamamlandı |
| 5 | **Marten conjoined açılışı** | Yüksek | **TEK YÖNLÜ KAPI** | ✅ Tamamlandı (#226 hat + #227 kapı) |
| 6 | İkinci okul kontrol listesi | Orta | Evet | ✅ Tamamlandı — izolasyon üç gerçek okulla ölçüldü |

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

### Adım 3 — ölçülen kural, tahmin edilenden farklı çıktı

Planda "user PUT'ları GET+merge'den geçsin" yazıyordu. Ölçüm bunu **daraltıyor**: sorun kısmi
gövde değil, gövdede **`attributes`** bulunması. Aynı kullanıcıda arka arkaya (Keycloak 26.7.0):

| Gövde | `firstName` | `email` | öznitelik haritası | HTTP |
| --- | --- | --- | --- | ---: |
| `{"enabled": false}` | korundu | korundu | korundu | 204 |
| `{"email","firstName","lastName"}` | yazıldı | yazıldı | korundu | 204 |
| `{"attributes": {...}}` | **NULL** | **NULL** | **tümüyle değişti** | 204 |

`attributes` gövdeye girdiği anda Keycloak'ın *declarative user profile* sağlayıcısı isteği tam
profil yazımı sayıyor. **Üç istek de 204 dönüyor**; kayıp ne çağıranda ne logda görünüyor —
#190'da haftalar sonra "personel listesinde ad sütunu boş" diye ortaya çıkmıştı.

Bu yüzden **her PUT'u GET+merge'e çevirmek yanlış olurdu**: kısmi yazma hem daha ucuz hem
eşzamanlılıkta daha güvenli (iki farklı alanı aynı anda güncelleyen iki istek birbirini ezmez).
Doğru ayrım semantiktedir ve kodda iki yola oturur:

- **Profil alanı** → `PatchUserFieldsAsync` (kısmi gövde; `attributes` görürse **fırlatır**)
- **Öznitelik** → `MergeUserAttributesAsync` (gövde taze bir GET'ten kurulur)

Kural yorumda değil, fırlatan bir sınıfta: `KeycloakUserWritePolicy`. Kilitleyen test
`KeycloakUserWriteDriftTests` — kuralın kendisi davranışsal ölçülüyor, çağrı yerinde durduğu
ayrıca kontrol ediliyor. (İlk sürüm yalnız kaynak taramasıydı ve negatif kontrolde **yakalamadı**:
koruma `if (false && …)` ile öldürüldüğünde test yeşil kalıyordu.)

Uçtan uca doğrulandı: API'den branş değişimi sonrası Keycloak'ta `firstName`, `lastName`,
`email`, `business_id` yerinde kaldı, `branch_codes` eklendi.

**Artık temizlendi.** Dev realm'inde 7 kullanıcının 6'sı hâlâ `institution_id` özniteliği
taşıyordu. Öznitelik **atıldı** — o claim her istekte siliniyor (adım 2) ve hiçbir kod onu
yazmıyor (`InstitutionClaimAuthorityTests`) — ama duran bir kopya, ileride birinin onu yeniden
otorite sanmasına davetiye çıkarır. Yazılmamışın yanında **durmaması** da gerekiyordu.

Silme işi elle yapılamaz: Keycloak konsolundan yalnız `attributes` göndermek kullanıcının adını
ve e-postasını siler (yukarıdaki tablo). Bu yüzden ayrı bir uç var —
`POST /api/security/users/purge-institution-attribute` — ve öznitelik yazan normal yoldan geçer.
Ölçüldü: 6 silindi, profiller ve `branch_codes`/`business_id` yerinde kaldı; ikinci koşu 0 sildi
(idempotent).

### Adım 5, ikinci yarı — kapı açıldı

Kapı açıldı: `DocumentTenancyPolicy` (haritadan damga), `Events.TenancyStyle = Conjoined`,
`Advanced.DefaultTenantUsageEnabled = false`. Ölçülenler, tahmin edilenlerden farklı çıktı.

**İzolasyon çalışıyor — ölçüldü.** Tam yetkili (77 izin) ikinci bir kiracıyla bakıldığında:
öğrenci 0, sözleşme 0, devamsızlık 0; işletme 100 (paylaşımlı, tasarım gereği). Aynı istekler
birinci kiracıda 121/4/12 dönüyor. Yazma tarafı da kendiliğinden doğru damgalanıyor: yeni
öğrenci ve onun asenkron tüketici çıktıları (`StudentNameView`, iki modülde) kiracıyı kuyruktan
devraldı. **219 çağrı yerinin hiçbirine dokunulmadı.**

**`ApplyAllDatabaseChangesOnStartup()` kullanılamıyor — Marten'ın göç betiği kendisiyle
çelişiyor.** Conjoined deltası `shared.mt_events` üzerindeki
`fkey_mt_events_stream_id_tenant_id` kısıtını **iki kez** ekliyor; ikincisi
`42710: constraint already exists` ile patlıyor ve bütün göçü geri alıyor. Açılışta denendiğinde
sonuç `MartenSchemaException` ve API'nin hiç ayağa kalkmaması oldu. Bu, "takılma" sanılan
davranışın gerçek sebebiydi — log Aspire panosuna gittiği için görünmüyordu. Göç bu yüzden
açılışta değil, elden uygulanan iki betikle yapılıyor (`dagitim-on-kosullari.md`).

**AutoCreate tembeldir** ve kiracılık geçişinde bu yarım göç demek: dokunulmamış `mt_streams`
eski `(id)` PK'sıyla kalır, damgalama FK yüzünden geri alınır. Betik tek transaction'da
çalıştığı için bu tuzak kapanıyor. Boş veritabanı betiklere ihtiyaç duymaz — kıran şey yalnız
var olan tablonun **deltasıdır**.

**Aynı tembellik ters yönde çalışıyor ve kapının "tek yönlü" olmasının asıl sebebi bu.** Göç
edilmiş bir veritabanına kiracılık öncesi kod bağlanırsa Marten şemayı kendi beklentisine uydurur
ve **damgayı siler**. Ölçüldü: üç GET isteğinden sonra `tenant_id` taşıyan tablo sayısı 49'dan
46'ya düştü, `mt_doc_studentprofile` birincil anahtarı `(tenant_id, id)`'den `(id)`'ye döndü.
Satırlar yerinde kaldı (121), kiracı bilgisi gitti. Hata yok, log temiz, uçlar 200.

Sürüm geri alınırsa göç geri alınmış olmaz: kiracı bilgisi kolondaydı, kolon düşürüldü. İleri
sürüme dönmek onu geri getirmez — yalnız boş (`*DEFAULT*`) olarak yeniden yaratır. Bu dağıtımdan
sonra eski imaja dönülmez; ileri düzeltme yapılır ya da yedekten geri yüklenir.

**Kapının en pahalı bulgusu yetkilendirmedeydi.** `DefaultTenantUsageEnabled = false` açılınca
DI'dan gelen `IQuerySession` kiracısız kaldı ve `UserPermissionProvider` sorgusu fırlattı.
İstisna `PermissionClaimsTransformation` içinde **yutuluyordu**: sonuç sessizdi ve tam ters
yöndeydi — yetkilendirme token'daki rollere geri düştü, yani ADR-0003 adım 2'nin kapattığı yol
yeniden açıldı. Ölçüldü: **devre dışı bırakılmış bir hesap 22 izinle öğrenci verisi okumaya
devam ediyordu (HTTP 200).**

İki düzeltme yapıldı ve ikisi de kilitlendi:

- Kimlik katmanı kiracıyı **açıkça** verir (`store.QuerySession(TenantResolution.Platform)`);
  `UserAccount` zaten kiracı damgası taşımaz, hangi kiracıyla okunduğu sonucu değiştirmez
- **Arıza token'a düşmez.** "Kayıt yok" (token yedeği meşru) ile "kayıt okunamadı" (arızada
  kapalı kalınır) artık ayrı durumlar

**Arka plan işleri kiracıyı istekten devralamaz.** Aylık maaş ve aylık rapor zamanlayıcıları
`ITenantDirectory` ile kiracı kiracı dolaşıyor; sözleşme altyapıda, uygulaması Institution
modülünde (şema izolasyonu korunur). Bir okulun koşusu patlarsa diğerleri devam eder.

**Anonim istek platform kiracısında çalışır.** Uygulamada tek anonim uç var: davet tamamlama.
Kiracı verilmezse istisna belge erişiminde değil **session açılırken** atılır — handler ilk
satırını çalıştıramadan 500 döner. Bu "kiracı uydurmak" değildir: anonim çağıranın dokunabildiği
belgeler kimlik katmanındadır ve damga taşımazlar. `UserInvitation` bu yüzden `Tenant`'tan
`Identity`'ye alındı — daveti tamamlayan kişinin henüz kiracısı yoktur, daveti okumadan da
kiracı bilinemez; `UserAccount` ile aynı döngüsellik. Liste `AnonymousEndpointDriftTests` ile
kapalı: kiracıya ait belgeye dokunan yeni bir anonim uç sessizce **boş sonuç** görürdü.

**Kalıcı nöbetçiler** (tek seferlik geçiş kontrolü değil): `TenantStampIntegrityTests` (hiçbir
satır `*DEFAULT*` kovasında kalmaz, `mt_streams` PK'sı `(tenant_id, id)`, sınıflandırma tabloya
iki yönde de yansır), `TenantlessSessionDriftTests` (argümansız session açma yok; istek dışında
çalışan sınıfa session enjekte edilmez), `AnonymousEndpointDriftTests`,
`IdentityLayerTenancyTests`.

### Adım 6 — ikinci okul gerçekten açıldı, üç sızıntı çıktı

Kiracılık kapısı (adım 5) satırları ayırıyor. Adım 6, ikinci bir okulu **gerçekten açıp** iki
müdürle bakmakla başladı ve kiracılığın **koruyamadığı** bir yüzey buldu.

**Kiracılık tarafı temiz.** Üç okul, üç müdür:

| Müdür | Öğrenci | Sözleşme | Gördüğü kurum |
| --- | ---: | ---: | ---: |
| Atatürk MTAL | 121 | 4 | 1 |
| Gazi MTAL | 2 | 0 | 1 |
| Cumhuriyet MTAL | 0 | 0 | 1 |

Kimlikle çapraz erişim iki yönde de 404; işletme kataloğu (paylaşımlı) üçünde de 100.

**Ama `Institution` belgesi kiracının KENDİSİDİR ve damga taşımaz** — conjoined onu süzmez.
`/api/institutions/{institutionId}` altındaki uçlar hedefi **istekten** alıyordu ve kimse
aktörün kapsamıyla karşılaştırmıyordu. Gazi'nin müdürüyle ölçüldü:

| İstek | Sonuç (önce) | Sonuç (sonra) |
| --- | --- | --- |
| `GET /api/institutions/{Atatürk}` | **200** — kayıt + **7 kişilik personel listesi** | 422 |
| `PATCH /api/institutions/{Atatürk}` | **200** — okulun **adı değişti** | 422 |
| `POST /api/institutions/{Atatürk}/staff` | **201** — personel **eklendi** (7→8) | 422 |
| `GET /api/institutions` | iki okul | 1 okul |
| `POST /api/security/users` + yabancı `institutionId` | **201** | 422 |

Sonuncusu ADR-0001'in kendi cümlesinin ihlaliydi: *izin erişimi açar, kapsamı belirlemez.*
`ChangeUserInstitution` `UserInstitutionScopePolicy` ile korunuyordu ama **oluşturma açıktı** —
kilitli kapının yanındaki açık pencere.

**Çözüm kapsamdadır, izinde değil.** Karar saf `InstitutionScopePolicy` içinde; uygulanışı
`IInstitutionScoped` + Wolverine middleware (`IContractPeriodScoped` ile aynı idiom). Kontrol
mesaj **tipine** bağlı olduğu için yeni bir uç eklemek yetmez, mesajın arayüzü taşıması gerekir;
kalan boşluğu `InstitutionScopeDriftTests` kapatır.

**Okumada da çalışır** — alan (branş) kapsamının aksine. Alan şefinin başka alanın dağıtımını
görmesi bilinçli olarak açıktı; başka *okulun* personel listesini görmek değildir.

### İkinci okul nasıl açılır (kontrol listesi)

Delik kapanınca yeni bir sorun çıktı: `CanAssign`'ın üç kuralı birlikte okunduğunda
**ikinci okulun ilk kullanıcısını bağlayabilecek kimse yoktu** — kapsamsız aktör yazamaz,
A'nın müdürü B'ye yazamaz. Kural doğruydu, eksik olan **bilinçli bir istisnaydı**:

`platform:tenant:manage` — kurum sınırının üstünde çalışma. `platform:` öneki hiçbir okul
rolünde yoktur (`PlatformScopeMappingTests`), bugün yalnız `SystemAdmin`'dedir ve bireysel
atanamaz.

1. **Okulu aç:** `POST /api/institutions` — artık `institution:manage` değil
   `platform:tenant:manage` ister. Yeni okul açmak kurum İÇİ bir iş değildir; ölçüldü, eski
   hâliyle bir okul müdürü ikinci okulu kendisi yaratabiliyordu
2. **İlk müdürü oluştur:** `POST /api/security/users` + `institutionId` = yeni okul.
   Kapsam muafiyeti burada devreye girer
3. **Doğrula:** yeni müdür giriş yapınca `tenantId` yeni okuldur, öğrenci/sözleşme sayısı
   **0**, kurum listesi **1**

`SystemAdmin` bu iş için kurum verisi yetkisi ALMAZ: `institution:view/manage` verilmedi, yani
okul listesini bile görmez. Bağlama yetkisi izinden değil kapsam muafiyetinden gelir.

## Sonuçlar

**Kazanç:** izolasyon disiplinden altyapıya taşınır; filtre unutmak veri sızdırmak yerine boş
sonuç üretir. Yeni belge eklemek kiracı kararını zorunlu kılar.

**Bedel:** yazma hattının tamamı kiracı taşımak zorunda; test ve seeder kiracı kurulumu ister;
`AnyTenant`/`TenantIsOneOf` yeni bir bypass yüzeyi açar ve ADR-0001 disiplininde ayrı izinle
korunmalıdır.

**Ölçüldü — async projeksiyon yeniden kurulumu gerekmedi.** Kaynaklar "genelde önerilir" diyordu,
kesin kural vermiyordu; dokümana güvenilmedi. Göç ve damgalama sonrası daemon mevcut
ilerlemesinden devam etti: `mt_event_progression` içinde `AttendanceView:All` = 39,
`HighWaterMark` = 39 — tam yetişmiş, sıfırdan kurulum yok. Bu **N=1 ve 39 olaylık** bir ölçümdür;
çok akışlı büyük bir olay deposu için aynı sonucu garanti etmez.

## Kapsam dışı

- **`Business.InstitutionId`'nin kaldırılması bir index işi değil, ayrı bir domain
  migration'dır** (#150 index işini kapsar): aynı vergi numaralı kayıtların birleştirilmesi ve
  okula bağlı alanların ilişki entity'sine taşınması.
- Çok okullu işletme durum yönetimi (#151) — "farklı kurumlardan iki bildirimle işletme küresel
  kapanır" kuralı teknik değil kurumsal-politiktir; ikinci okul gerçekleşmeden tasarlanması
  spekülasyondur.
