# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

**Birincil — okul tarafı.** Kurum Müdürü (`InstitutionManager`), Müdür Yardımcısı
(`DeputyDirector`), Alan Şefi (`DepartmentHead`), Koordinatör Öğretmen (`Teacher`),
Kurum Yetkilendirdiği Personel (`InstitutionStaff`). Mesai içinde, masaüstünde,
gün boyu açık. İş yükü tablo ve onay ağırlıklı: devamsızlık kaydı, dekont onayı,
koordinasyon dağıtımı, sözleşme takibi, dönem notu.

**İkincil — işletme tarafı.** İşletme Yöneticisi (`CompanyManager`), Usta Öğretici
(`MasterTrainer`), İşletme İK (`CompanyHR` — zorunlu değil). Seyrek girer, tek amaçlı
görev yapar: devamsızlık bildir, dekont yükle, sözleşme imzala, ücretli izin onayla.

**Üçüncül — öğrenci ve veli.** `Student`, `Parent`. En geniş kitle, en seyrek giriş.
Kendi devamsızlığını/maaşını görme, ücretli izin başvurusu, sözleşme onayı.

Kullanım yoğunluğu sırası kullanıcı tarafından doğrulandı: **okul > işletme > öğrenci/veli.**
Kurum üstü rol `SystemAdmin` (ulusal parametre girişi) günlük kullanıcı değildir.

## Product Purpose

MESNET (Mesleki Eğitim Stajları Nitelikli, Eşgüdümlü Takip Sistemi) mesleki eğitim
staj süreçlerini uçtan uca dijitalleştirir: staj sözleşmesi ve imza zinciri,
devamsızlık takibi ve sağlık raporu, dekont/maaş onayı, staj fesih ve yeni işletmeye
yerleşme, koordinatör öğretmen dağıtımı ve ders programı, dönem notu, raporlama ve
resmî form PDF'i (QuestPDF).

Başarı: bugün kağıt, Excel ve WhatsApp üzerinde yürüyen staj takibinin tek sistemde,
her hükmü izlenebilir biçimde yürümesi.

## Positioning

Dört mekanizma birlikte doğrulandı:

1. **e-Okul/MEBBİS'in kapsamadığı staj katmanı.** Resmî sistemler öğrenci kaydını
   tutar; işletme–sözleşme–dekont–koordinasyon zincirini tutmaz.
2. **Okul + işletme + veli aynı onay zincirinde.** Tek taraflı hüküm doğmaz: ücretli
   izin işletme ve okul onayından geçer, sağlık raporu girişi okul onayına tabidir,
   ödemeyi yapan taraf kendi kesintisini tek başına kaldıramaz. Bu ürünün ayırt edici
   mekanizmasıdır — "giriş geniş, hüküm dar".
3. **Denetlenebilir olay kaydı.** Event sourcing: kim ne zaman neyi onayladı, geri
   alınabilir mi. Resmî denetimde kanıt üretir.
4. **Kağıt/Excel/WhatsApp'ın yerine geçme.** Doğrudan rakip ürün değil, mevcut elle
   yürüyen süreç alternatiftir.

## Operating Context

- **Kiracı = okul.** Çok kiracılı (Marten conjoined tenancy); her okul kendi verisini
  görür. Kurum kapsamı ayrı bir kontrol katmanıdır.
- **Akademik dönem eksenlidir.** Tüm kayıt ve sorgular `AcademicPeriodId` taşır.
  Kapalı dönem **salt okunur** — tüm yazma yolları (buton, form, handler) kapanır.
- MEB terminolojisi: "1. Dönem" / "2. Dönem" (Güz/Bahar değil).
- Planlama ekseni il + ilçe. Bakanlık/ulusal aktör katmanı kapsam dışı.
- Kullanıcılar sisteme **davet akışı** ile girer: yetkili davet oluşturur → müdür /
  müdür yardımcısı onaylar → davet edilen hesabını tamamlar.
- Kimlik Keycloak'ta (OAuth2/OIDC, PKCE public client). 11 realm rolü.
- Bazı işlemler resmî belge üretir (Form 8, dönem not fişi, ödeme listesi) — PDF çıktı
  ekranın değil kağıdın kuralına uyar.

## Capabilities and Constraints

**Yığın:** Vue 3 + TypeScript + Quasar 2 SPA (Vite, pnpm), Pinia, vue-router.
Harita için Leaflet + PostGIS, grafik için ECharts, sürükle-bırak için vue-draggable-plus.
Backend .NET 10 modüler monolit (Wolverine + Marten + PostgreSQL).

**Yüzey:** 38 sayfa / 12 alan — admin (kullanıcı, rol, izin kapsamı), attendance
(devamsızlık, ücretli izin), business, contract, coordination (11 sayfa: dağıtım,
ders programı, işletme saatleri, beceri sınavı, haftalık ziyaret, dönem notu,
etkinlik raporu, iş yükü/koordinasyon yapılandırması), enrollment, institution,
internship (genel bakış, onaylarım, fesihler), payment (ödeme, maaş yapılandırması),
reporting (rapor, belgeler), dashboard, öğrenci listesi/formu, hata sayfaları.

**Bağlayıcı arayüz kuralları (mevcut, korunur):**
- Arayüz dili **Türkçe**; Türkçe karakterler ASCII'ye düşürülmez. Backend enum adları
  İngilizce kalır, ekranda Türkçe karşılığı gösterilir (SmartEnum `Name`/`Slug`).
- Oluştur/düzenle formları **ayrı route sayfası**, modal değil. Kısa aksiyon formları
  (reddet, imzala, fesih, belge yükle, silme onayı) sağdan kayan `FormDialog` yan paneli.
- Yalnız ikonlu her buton `aria-label` + `<q-tooltip>` taşır; `title` attribute kullanılmaz.
- Boş durum bir hata değildir: uyarı ikonu değil nötr ikon + eylem çağrısı.
- Listeler sunucu taraflı sayfalama (`AppTable` + `useServerPagination`).
- `<script setup>` zorunlu; 300 satırı aşan bileşen composable'a bölünür.

**Karara bağlandı — beyaz etiket küratörlü paletle çözüldü.** Kiracı (okul) kendi
markasını seçer ama **serbest renk giremez**: kodda yaşayan ve testle kilitlenen sekiz
önceden ölçülmüş seçenekten birini seçer. Veritabanında **ham hex saklanmaz**, kümeye bir
**anahtar** yazılır (`Institution.BrandPaletteName` — nullable; seçim yapmamış kurum ve
tanınmayan değer varsayılan palete düşer, eski kayıtlar okunur kalır). Böylece veriye düşen
bozuk bir değer kontrastı kıramaz. **Anlamsal renkler kiracıya göre değişmez:**
`positive` / `negative` / `info` / `warning` ve Resmî Hardal (`accent`) sabittir — bunlar
marka ifadesi değil sistem anlamıdır. Yalnız `primary` ve `secondary` kayar; ölçülmüş bütün
anlamsal kontrastlar bu sayede her kiracıda geçerli kalır. Bağlayıcı kapı her seçenekte
aynıdır: türetilmiş ikincil butonun beyaz metni (en dar 4,95:1, eşik 4,5:1).

**Bugün var olan:** palet kümesi (`InstitutionBrandPalette`, 8 seçenek), kurum alanı,
katalog ucu `GET /api/institutions/brand-palettes` (`institution:view`), atama ucu
`PUT /api/institutions/{institutionId}/brand-palette` (`institution:manage`), kilitleyen
kontrast testi ve **çalışma zamanı uygulaması** (`utils/brandTheme.ts` → `--q-primary` /
`--q-secondary`, kurum profili yüklenirken tetiklenir; geçersiz değerde derleme zamanı
varsayılanına düşer). **Bugün olmayan:** arayüzde palet seçici — hiçbir sayfa katalog/atama
uçlarını çağırmıyor, palet yalnız API üzerinden değişiyor. Beyaz etiketin logo tarafı da
açık: web logosu ve favicon yok.

**Açık/karara bağlanmamış:** Koyu tema değişkenleri tanımlı (`$dark`, `$dark-page`)
ama **kullanılmıyor** — koyu tema hâlâ ürün kararı değildir.

## Brand Commitments

**Ad:** MESNET. Üst barda düz metin olarak geçiyor; web arayüzünde logo kullanılmıyor.
Tek marka varlığı `src/MESNET.Common.Infrastructure/Email/Assets/logo.png` (e-posta şablonu).

**Bağlayıcı karar — okul bazlı beyaz etiket, küratörlü paletle.** Kiracı = okul olduğu
için her okul kendi rengini görmek ister; ama seçim **serbest değildir**: sekiz küratörlü
seçenekten biri seçilir ve kayda hex değil **anahtar** düşer. Karar **yalnız rengi** kapsar
— logo tarafı hâlâ açıktır (web logosu ve favicon yok, tek marka varlığı e-posta logosudur).

**Varsayılan palet — `Lacivert` / "Mührü Lacivert"** (sekiz seçenekten biri, bugün
`src/WebUI/src/assets/quasar-variables.sass`ta derlenen değerler): derin lacivert `#1E3A5F`
(birincil), çelik mavisi `#4A6FA5` (ikincil; küratörlü kümedeki OKLCH türevi `#4870A4`, bir
tık farklı — 5,11:1 ↔ 5,09:1, ikisi de eşiği geçer), hardal `#C9A227` (vurgu — beyaz
üzerinde 2,4:1, metin rengi olarak kullanılmaz; **kiracıya göre değişmez**). Anlamsal
renkler beyaz metinle AA sağlayacak şekilde elle seçilmiş ve kiracıdan bağımsızdır.
Tipografi: sistem yığını, web fontu indirilmiyor (statik dağıtım + ilk boyama gecikmesin
diye). Gövde 14px'te sabit — büyütmek 17 sayfalık tablo düzenini kaydırır. Köşe yarıçapı 8px.
Bu palet resmî bir kurum kılavuzundan gelmiyor; küratörlü kümenin **varsayılanıdır**.

MEB kurumsal kimlik kılavuzu dayatması **yoktur** (kullanıcı doğruladı).

## Evidence on Hand

- Çalışan uygulama: 38 sayfa, canlı dev yığını (WebUI :5173, Keycloak, PostgreSQL, MinIO).
- 1505 backend testi, 183 frontend vitest testi, 228 BDD API testi — hepsi yeşil.
- Docusaurus doküman sitesi `src/Docs/` — tek doğruluk kaynağı: aktörler, izin matrisi,
  iş kuralları, senaryolar, C4 diyagramları, ADR-0001/0002/0003.
- Gerçek resmî form üretimi (Form 8, dönem not fişi) QuestPDF ile.
- E-posta logosu (yukarıda).

**Yok — uydurulmayacak:** müşteri referansı, pilot okul adı, kullanım istatistiği,
fiyatlandırma, lisans, SLA, canlı dağıtım iddiası, ekran görüntüsü/tanıtım varlığı,
web logosu, favicon.

## Product Principles

1. **Hüküm doğuran her ekran iki tarafı gösterir.** Kim girdi, kim onayladı, hangi
   aşamada — tek taraflı sonuç doğuran bir arayüz ürünün mekanizmasını bozar.
2. **Yoğun okul kullanıcısı önce gelir.** Tarama hızı, sütun tutarlılığı, klavye ve
   tablo ritmi; işletme/öğrenci yüzeyleri tek amaçlı ve seyrek kullanıma göre kurulur.
3. **Kapsam görünür olur.** Hangi okulun, hangi alanın, hangi dönemin verisine
   bakıldığı her zaman ekranda okunur; kapalı dönem salt okunurluğu gizlenmez.
4. **Türkçe tam ve doğru.** Karakterler eksiksiz, MEB terminolojisi esas.
5. **Kimlik kiracıdan gelir.** Marka sabit varsayılmaz; renk ve logo değişebilir
   kabul edilerek tasarlanır. Renk küratörlü sekiz seçenekten gelir — serbest hex değil;
   anlamsal renkler kiracıdan bağımsız sabittir.

## Accessibility & Inclusion

Hedef **WCAG 2.x AA**. Uygulanan ve korunacak taahhütler:

- Anlamsal renkler beyaz metinle ≥ 4,5:1 verecek şekilde elle seçildi (`$positive`
  5,2:1, `$negative` 6,6:1, `$info` 5,8:1, `$warning` 4,8:1 — Quasar varsayılanı
  #F2C037 ~1,9:1 idi). `$accent` metin rengi olarak yasak.
- Yumuşak/-strong ton çiftleri kendi zeminlerinde ≥ 4,5:1; `color-mix()`
  desteklenmeyen tarayıcılarda düz hex yedeği var.
- `eslint-plugin-vuejs-accessibility` derlemede aktif.
- Yalnız ikonlu butonlarda `aria-label` + görsel `<q-tooltip>` zorunlu; `title`
  attribute güvenilir sayılmaz.
- Beyaz ekran yasağı: `index.html` içinde Vue'suz çalışan "Oturum doğrulanıyor…"
  iskelesi, boot hiç tamamlanmasa bile görünür (#136).
