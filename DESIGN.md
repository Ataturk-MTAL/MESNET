---
name: MESNET
description: Mesleki eğitim staj takibinin okul, işletme ve veliyi aynı onay zincirinde buluşturan kurumsal yönetim arayüzü.
colors:
  muhur-lacivert: "#1E3A5F"
  celik-mavisi: "#4A6FA5"
  resmi-hardal: "#C9A227"
  onay-yesili: "#2E7D5B"
  ret-kirmizisi: "#B3261E"
  bilgi-mavisi: "#3E6B89"
  uyari-hardali: "#9A6B00"
  notr-govde: "#465A73"
  notr-zemin: "#EDEFF2"
  kagit: "#FFFFFF"
  ayrac: "rgba(30, 58, 95, .14)"
  koyu-yuzey: "#16233A"
  koyu-sayfa: "#0F1826"
typography:
  display:
    fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Inter', Roboto, 'Helvetica Neue', Arial, sans-serif"
    fontSize: "1.75rem"
    fontWeight: 700
    lineHeight: "2.25rem"
    letterSpacing: "-.01em"
  headline:
    fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Inter', Roboto, 'Helvetica Neue', Arial, sans-serif"
    fontSize: "1.375rem"
    fontWeight: 600
    lineHeight: "1.875rem"
    letterSpacing: "-.01em"
  title:
    fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Inter', Roboto, 'Helvetica Neue', Arial, sans-serif"
    fontSize: "1.125rem"
    fontWeight: 600
    lineHeight: "1.625rem"
    letterSpacing: "-.005em"
  body:
    fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Inter', Roboto, 'Helvetica Neue', Arial, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: "1.375rem"
    letterSpacing: "normal"
  label:
    fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Inter', Roboto, 'Helvetica Neue', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: 400
    lineHeight: "1.25rem"
    letterSpacing: "normal"
rounded:
  sm: "4px"
  md: "8px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "48px"
components:
  button-primary:
    backgroundColor: "{colors.muhur-lacivert}"
    textColor: "{colors.kagit}"
    rounded: "{rounded.md}"
    padding: "6px 18px"
    typography: "{typography.body}"
  button-primary-hover:
    backgroundColor: "#1a3354"
    textColor: "{colors.kagit}"
    rounded: "{rounded.md}"
  button-cancel:
    backgroundColor: "transparent"
    textColor: "#616161"
    rounded: "{rounded.md}"
    padding: "6px 18px"
  card-surface:
    backgroundColor: "{colors.kagit}"
    textColor: "#212121"
    rounded: "{rounded.md}"
    padding: "{spacing.md}"
  input-outlined:
    backgroundColor: "{colors.kagit}"
    textColor: "#212121"
    rounded: "{rounded.md}"
    height: "40px"
    typography: "{typography.body}"
  badge-status-pending:
    backgroundColor: "{colors.uyari-hardali}"
    textColor: "{colors.kagit}"
    padding: "4px 8px"
    typography: "{typography.body}"
  badge-status-active:
    backgroundColor: "{colors.onay-yesili}"
    textColor: "{colors.kagit}"
    padding: "4px 8px"
    typography: "{typography.body}"
  badge-status-negative:
    backgroundColor: "{colors.ret-kirmizisi}"
    textColor: "{colors.kagit}"
    padding: "4px 8px"
    typography: "{typography.body}"
  notice-readonly:
    backgroundColor: "{colors.notr-zemin}"
    textColor: "#424242"
    rounded: "{rounded.md}"
    padding: "{spacing.sm}"
    typography: "{typography.label}"
---

# Design System: MESNET

## Overview

**Creative North Star: "İmza Zinciri"**

Her ekran tek bir soruyu cevaplar: **şimdi kimin sırası?** MESNET'in ayırt edici mekanizması
tek taraflı hükmün doğmaması — ücretli izin işletme ve okul onayından geçer, sağlık raporu
girişi okul onayını bekler, dekont dört ayrı elden geçer. Arayüz bu zinciri gizlemez,
görselleştirir: aşama rozeti, bekleyen taraf ve sıradaki eylem birincil katmandır; tablonun
kendisi ikincil taşıyıcıdır. Bir kaydın hangi halkada durduğu, satıra bakan kişinin ilk
okuduğu şey olmalıdır.

Kişilik ölçülü ve kesin — "resmî ama nazik". Mührü lacivert ağırlığı taşır, çelik mavisi
ikincil eylemleri üstlenir, anlamsal renkler yalnız durum bildirir. Hiçbir eleman bağırmaz;
yükümlülük doğuran bir buton görsel olarak da ağır durur, geri alınabilir bir eylem hafif
kalır. Yoğunluk okul kullanıcısının gerçek sahnesine ayarlıdır: gün boyu açık kalan bir
pencere, 14px gövde, sabit genişlikli rakamlar, tarama hızını bozmayan sabit sütun ritmi.

Sistem dört şeyi bilerek reddeder. **Eski kamu yazılımı görünümü** — sıkışık gri tablo,
11px metin, kenarlık yığını: MESNET'in yerine geçtiği şey tam olarak budur, ona benzemek
yenilgidir. **Çiğ Material varsayılanı** — Quasar'ın kutudan çıkan mavisi, 4px yarıçapı,
devasa display ölçeği tema tarafından bilerek ezildi; şablonlara geri sızmamalıdır.
**Tüketici SaaS makyajı** — gradyan kahraman bölümü, cam efekti, pazarlama dili: bu bir iş
aracıdır, ikna yüzeyi değil. **Yoğun analitik panosu estetiği** — koyu zemin, neon grafik,
KPI duvarı: buradaki veri hüküm üretir, gösteri değil.

**Key Characteristics:**
- Aşama ve sorumlu taraf her satırda okunur; onay zinciri gizlenmez
- Düz yüzeyler, tonal derinlik — gölge yalnız gerçekten yüzen katmanda
- Renk anlam taşır; dekoratif renk yoktur
- Türkçe tam ve eksiksiz; MEB terminolojisi esas
- Kimlik kiracıdan gelir — primary ve secondary kayar, anlamsal renkler sabit kalır
- Kontrast ölçülür, tahmin edilmez (metin eşiği 4,5:1)

## Colors

Palet iki marka rengi + altı anlamsal rol + lacivertle tonlanmış nötrlerden oluşur; hiçbir
nötr saf gri değildir, hepsi mührü lacivertle tonlanır.

### Primary
- **Mührü Lacivert** (`{colors.muhur-lacivert}`): Üst bar, birincil butonlar, aktif menü
  öğesi, odak halkası. Uygulamanın ağırlık merkezi. 110 kullanım noktası bu tek değişkene
  bağlıdır — tema değiştiğinde hepsi birlikte kayar.

### Secondary
- **Çelik Mavisi** (`{colors.celik-mavisi}`): İkincil eylemler ve "tamamlanmış" terminal
  aşama rozeti. Laciverdin yükünü taşımadan aynı aileden konuşur.

### Tertiary
- **Resmî Hardal** (`{colors.resmi-hardal}`): **Sıra sizde** sinyali — kullanıcının kendi
  eylemini bekleyen satır veya rozet. Beyaz üzerinde 2,4:1 verir, yani ham hâliyle metin
  ya da grafik rengi olamaz; kullanım daima ölçülmüş türevlerden geçer
  (`.bg-accent-soft`, `.text-accent-strong` #796117 → 4,98:1, `.bg-accent-strong`).
  Bugünkü tüketicileri: `StatCard`ın `tone="accent"` ekseni (panelde yalnız
  "sıra sizde" sayacı, `DashboardPage`), kullanıcı yönetimindeki bekleyen rozet.
  `color` prop'uyla verilmez — `color="accent"` saf hardal metin üretirdi.

### Neutral
- **Nötr Gövde** (`{colors.notr-govde}`): Anlamsal durum taşımayan kategori rozetleri
  (sektör, rol, kaynak, form tipi). Beyaz metinle 7,1:1.
- **Nötr Zemin** (`{colors.notr-zemin}`): Yumuşak zemin katmanı; salt-okunur bildirimi,
  pasif satır, tablo başlığı bölgesi.
- **Kağıt** (`{colors.kagit}`): Kart ve sayfa zemini. Sistem açık zemin üzerine kuruludur.
- **Ayraç** (`{colors.ayrac}`): Tüm kenarlık ve ayraçlar. Saf siyah değil, laciverte
  tonlanmış — gri bulanıklık yerine zemine oturan yumuşak sınır.
- **Koyu Yüzey / Koyu Sayfa** (`{colors.koyu-yuzey}`, `{colors.koyu-sayfa}`): Tanımlı,
  **kullanılmıyor**. Koyu tema henüz ürün kararı değildir.

**Quasar gri ölçeği — kütüphane sabiti, tema token'ı DEĞİL.** Yukarıdaki nötrler temadan
türer; aşağıdaki griler türemez. `quasar/src/css/variables.sass` içinde sabittir, kiracı
rengi değişince kımıldamaz ve bu yüzden yalnız anlamsal yükü olmayan yerlerde kullanılır
(taslak/pasif/kapatılmış rozeti, ikincil metin, iptal butonu). Ölçek Material'ınkinden **bir
basamak kayıktır**; oranlar beyaza karşı ölçüldü (kontrast simetriktir — aynı sayı hem beyaz
zeminde metin hem beyaz metinli dolgu için geçerlidir):

| Quasar sınıfı | Hex | Beyazla oran | Metin (4,5:1) | Beyaz metinli rozet (4,5:1) |
| --- | --- | --- | --- | --- |
| `grey-5` | `#bdbdbd` | 1,88:1 | ✗ | ✗ |
| `grey` / `grey-6` | `#9e9e9e` | 2,68:1 | ✗ | ✗ |
| `grey-7` | `#757575` | 4,61:1 | ✓ teğet | ✓ teğet |
| `grey-8` | `#616161` | 6,19:1 | ✓ | ✓ |
| `grey-9` | `#424242` | 10,05:1 | ✓ | ✓ |
| `grey-10` | `#212121` | 16,10:1 | ✓ | ✓ |

### Anlamsal roller
- **Onay Yeşili** (`{colors.onay-yesili}`): Aktif, onaylandı, doğrulandı, ödendi. Beyaz metinle 5,2:1.
- **Ret Kırmızısı** (`{colors.ret-kirmizisi}`): Reddedildi, feshedildi, iptal. 6,6:1.
- **Bilgi Mavisi** (`{colors.bilgi-mavisi}`): İmzalandı, yüklendi, hesaplandı — bilgilendirici ara durum. 5,8:1.
- **Uyarı Hardalı** (`{colors.uyari-hardali}`): Askıda, fesih talebi, süre doldu, bekleyen onay. 4,8:1.

### Kiracı paleti (küratörlü)

Beyaz etiket kararı verildi ve **serbest renk girişi olmadan** çözüldü: kiracı (okul) kendi
markasını sekiz önceden ölçülmüş seçenekten birini seçerek belirler. Frontmatter'daki
`colors` bu kümenin **varsayılanını** tanımlar; kalan yedi seçenek yalnız primary ve
secondary'yi kaydırır, başka hiçbir token'a dokunmaz — bu yüzden aşağıdaki tablo hex taşır,
ayrı bir kümedir.

| Anahtar | Ad | Primary | Secondary |
| --- | --- | --- | --- |
| `Lacivert` | Mührü Lacivert (varsayılan) | `#1E3A5F` | `#4870A4` |
| `Deniz` | Deniz Mavisi | `#123A63` | `#3A70AA` |
| `Petrol` | Petrol Yeşili | `#0E4146` | `#387980` |
| `Orman` | Orman Yeşili | `#1B422C` | `#467B5B` |
| `Bordo` | Bordo | `#6B1F2E` | `#B54B5C` |
| `Erguvan` | Erguvan | `#4A2352` | `#885193` |
| `Indigo` | İndigo | `#2A3072` | `#5763C0` |
| `Antrasit` | Antrasit | `#2B3138` | `#5C656F` |

**Bağlayıcı kapı primary değil, türetilmiş secondary'dir.** Sekiz seçeneğin primary'si beyaz
metinle en az 11,2:1 verir — paletin ne kadar açık olabileceğini belirleyen sınır oradan
gelmez. Sınır ikincil butonun beyaz metnidir: en dar seçenek Orman Yeşili (`#467B5B`)
**4,95:1**, eşik 4,5:1. Palete yeni seçenek eklenirse önce o kapı ölçülür.

**Secondary türetme kuralı (OKLCH).** İkincil renk elle seçilmez, primary'den türetilir:
**L +19,2pp, kroma ×1,28, hue sabit**. Türetme sekiz seçeneğin hepsinde aynı ilişkiyi
korur; en dar ölçüm bu yüzden her zaman türetilmiş renkte çıkar.

Küme kodda yaşar (`InstitutionBrandPalette`) ve testle kilitlidir
(`BrandPaletteContrastTests`); hex ikinci kez tanımlanmaz, arayüz katalog ucundan okur.
Varsayılan satırın secondary'si bu OKLCH türevidir ve bugün derlenen çelik mavisinden bir
tık farklıdır (5,09:1 ↔ 5,11:1 — ikisi de eşiği geçer).

**Uygulama çalışma zamanındadır.** Kurum profili yüklenirken `applyBrandTheme`
`--q-primary` / `--q-secondary` değişkenlerini `document.documentElement` üzerine yazar;
`color-mix()` ile bu değişkenden türeyen bütün `-soft` / `-strong` / durum tonları onunla
birlikte kayar. İki değerden biri bile geçersizse hiçbiri uygulanmaz ve tema derleme zamanı
varsayılanına döner — yarım uygulanmış palet, primary'si bir kiracıdan secondary'si
başkasından gelen ölçülmemiş bir çift demektir. **Seçici arayüzdedir:** kurum ayarlarının
Kurum Bilgileri sekmesindeki "Kurum Teması" kartı geçerli paleti örnekle gösterir,
"Değiştir" butonu `institution:manage` ile korunur (`PermissionGuard`) ve seçenekleri
katalog ucundan okur. Kaydetmeden sonra kurum profili yeniden yüklenir; tema o tek kapıdan
tazelenir.

### Named Rules

**Küratörlü Palet Kuralı.** Kiracı serbest renk giremez; kodda yaşayan ve testle kilitlenen
sekiz seçenekten birini seçer. Veritabanı ham hex değil **anahtar** saklar — veriye düşen
bozuk bir değer kontrastı kıramaz, tanınmayan anahtar varsayılan palete düşer ve ölçülmemiş
bir renk hiçbir yüzeye ulaşamaz.

**Sabit Anlam Kuralı.** positive / negative / info / warning ve Resmî Hardal kiracıya göre
**değişmez** — bunlar marka ifadesi değil sistem anlamıdır. Yalnız primary ve secondary
kayar; ölçülmüş bütün anlamsal kontrastlar böylece her kiracıda geçerli kalır.

**Tek Ses Kuralı.** Resmî Hardal yalnız "sıra sizde" anlamına gelir ve bir ekranda en çok
bir bağlamda görünür. Nadirliği anlamın kendisidir; dekoratif vurgu olarak kullanılırsa
sinyal ölür.

**Türetme Kuralı.** Hiçbir yumuşak ton elle yazılmaz. `-soft` / `-strong` çiftleri
`color-mix()` ile marka değişkeninden türetilir (`bg-positive-soft`, `text-warning-strong`).
Ham Quasar palet tonu (`bg-orange-1`, `text-blue-8`) tema dışına düşer ve kiracı rengi
değiştiğinde yerinde donar. **Bugünkü borç: 13 nokta.**

**Ölçülmüş Kontrast Kuralı.** Yeni bir renk çifti eklenmeden önce kontrast ölçülür. Metin
eşiği 4,5:1; `-strong` tonlar kendi `-soft` zeminlerinde bu eşiği geçer. Karışım oranı
değiştirilirse oran yeniden ölçülür — bu değerler tahmin değil, hesap.

**Renk Yalnız Kanıt Kuralı.** Renk hiçbir zaman tek sinyal değildir. Durum rozeti her zaman
metin etiketi taşır; renk körlüğünde bilgi kaybı olmaz.

**Kayık Gri Kuralı.** Quasar'ın gri ölçeği Material'ınkinden bir basamak kaymıştır:
`grey-6` = `#9e9e9e`, `grey-7` = `#757575`, `grey-8` = `#616161` — Material'da `#616161`
grey-700'dür. Metin için taban `grey-7`; `grey-6` ve `grey-5` metin olarak da beyaz metinli
rozet zemini olarak da kullanılamaz. Hex **her zaman** `quasar/src/css/variables.sass`tan
doğrulanır, ezberden yazılmaz: bu depoda iki bağımsız denetim aynı sınıf için farklı hex
bildirdi ve iki ölçüm birbirini tutmadı. Kayma bir kez daha unutulacaktır; dosyaya bakmak
tek güvenli yoldur.

## Typography

**Display Font:** Sistem yığını (`-apple-system`, `BlinkMacSystemFont`, `Segoe UI`, `Inter`,
`Roboto`, `Helvetica Neue`, `Arial`)
**Body Font:** Aynı yığın
**Label/Mono Font:** Ayrı yok — sayısal hizalama `font-variant-numeric: tabular-nums` ile çözülür

**Character:** Web fontu bilerek indirilmiyor: dağıtım statik kalsın ve ilk boyama gecikmesin
diye. Yığın Türkçe karakterleri (ç ş ğ ü ö ı İ) tam kapsar. Karakter nötr ve kurumsal —
tipografi burada kişilik taşımaz, okunurluk taşır; kişilik renk ve ritimde yaşar.

### Hierarchy
- **Display** (700, 1.75rem / 2.25rem, -.01em): Sayaç kartı değerleri, tek büyük rakam.
  Sayfa başlığı DEĞİL.
- **Headline** (600, 1.375rem / 1.875rem, -.01em): Sayfa başlığı (`PageHeader` içindeki `<h1>`).
- **Title** (600, 1.125rem / 1.625rem, -.005em): Bölüm ve kart başlıkları, panel toolbar'ı.
- **Body** (400, 14px / 1.375rem): Tablo hücresi, form etiketi, gövde metni. Rozet etiketi de
  bu boyutu kullanır.
- **Label** (400, 12px): Alt açıklama, caption, boş-durum metni, zaman damgası. Gri tonda.

### Named Rules

**Sabit Rakam Kuralı.** Sayı gösteren her yüzey `tabular-nums` kullanır (tablo hücreleri,
sayaç değerleri). Orantılı fontta "1" ile "8" farklı genişliktedir; sayı değişince sütun
kayar ve göz zıplar.

**14px Sabit Kuralı.** Gövde 14px'te sabittir. Büyütmek 38 sayfalık tablo düzenini kaydırır;
okunurluk sorunu varsa çözüm satır yüksekliği ve kontrasttır, punto değil.

**Sarma Kuralı.** Başlıklar `text-wrap: balance` (son satırda tek kelime kalmasın),
açıklama/caption `text-wrap: pretty`. Uzun düz metin, kod ve `<pre>` bu kuralın dışındadır.

## Layout

Kabuk kalıcı bir sol çekmece (`q-drawer`, `show-if-above`) + üst bar + `q-page-container`
üçlüsüdür. Çekmece dönem/yarıyıl seçicisini en üstte taşır — kapsam her zaman ekranda
okunur — ve altında izne göre süzülmüş, gruplanabilir menüyü listeler.

Sayfa gövdesi tek konvansiyona uyar: `<q-page padding>` (38 sayfanın 35'i). Sayfa başı her
zaman `PageHeader` — solda başlık + isteğe bağlı alt başlık, sağda eylem butonları.

Boşluk ritmi Quasar ölçeğidir: `xs 4px`, `sm 8px`, `md 16px`, `lg 24px`, `xl 48px`. Başlık
ile içerik arası `lg`, kart içi `md`, satır içi öğe arası `sm`.

Kırılma noktaları Quasar varsayılanıdır: `xs <600px`, `sm 600px`, `md 1024px`, `lg 1440px`,
`xl 1920px`. Uyarlama iki yerde davranış değiştirir: çekmece `md` altında örtüşür, yan-panel
formu (`FormDialog`) `sm` altında tam ekrana geçer (`maximized`).

Yoğunluk yüksektir ve öyle kalmalıdır: `dense` girdi ve tablo satırı varsayılan, sayfa başına
10/20/50 kayıt seçeneği. Okul kullanıcısı listeyi tarar, kaydırmaz.

### Named Rules

**Kapsam Görünür Kuralı.** Hangi dönemin ve hangi yarıyılın verisine bakıldığı her ekranda
okunabilir olmalıdır. Dönem kapalıysa salt-okunur bildirimi çekmecede kalıcı durur ve tüm
yazma yüzeyleri devre dışıdır — gizlenmez, açıklanır.

**Form Sayfa Kuralı.** Bir varlığın oluştur/düzenle formu **ayrı route sayfasıdır**, modal
değil (`/entity/new`, `/entity/:id/edit`). Kısa ve bağlamsal aksiyonlar (reddet, imzala,
fesih, belge yükle, silme onayı) sağdan kayan yan-panel (`FormDialog`) kullanır. Merkezî
modal bu sistemde yoktur.

## Elevation & Depth

Bu sistem **tonal katmanlamayla** derinlik kurar, gölgeyle değil. Yüzeyler dinlenirken düzdür
ve laciverte tonlanmış ince bir kenarlıkla ayrılır (`flat bordered` kart ve tablo). Katman
farkı `-soft` zemin tonlarıyla anlatılır ve bu tonlar tema renginden `color-mix()` ile türer —
kiracı rengi değişince derinlik dili de birlikte kayar. Gölge sabit bir renge bağlı olduğu
için beyaz etiketle ölçeklenmez; tonal katmanlama ölçeklenir.

Gölge yalnız **gerçekten yüzen** katmanda meşrudur: dialog, menü, örtüşen çekmece, yapışkan
üst bar. Bir kart, bir tablo veya bir bölüm hiçbir zaman kendi başına yükselmez.

### Shadow Vocabulary
- **Yüzen katman** (Quasar `elevated`, gölge rengi `{colors.muhur-lacivert}` ile tonlanır):
  Üst bar, dialog, menü, örtüşen detay çekmecesi.
- **Hover kaldırma** (`0 4px 12px rgba(0,0,0,.08)` + `translateY(-2px)`): Yalnız tıklanabilir
  sayaç kartında. **Bu bir istisnadır ve borç olarak işaretlidir** — tonal katmanlama kuralına
  aykırıdır, yeni bileşene kopyalanmamalıdır.

### Named Rules

**Tonal Derinlik Kuralı.** Bir yüzeyi öne çıkarmak gerekiyorsa gölge değil `-soft` zemin tonu
kullanılır. `box-shadow` yazmak, o elemanın gerçekten sayfanın üstünde yüzdüğünü iddia etmektir.

**Kenarlık Önce Kuralı.** Ayrım önce kenarlıkla (`{colors.ayrac}`), sonra zemin tonuyla,
en son gölgeyle kurulur. Üç katman aynı anda kullanılmaz.

## Shapes

Form dili yumuşak ama dikdörtgendir. Köşe yarıçapı iki adımdır: kart, girdi, menü ve buton
`{rounded.md}` (Quasar varsayılanı 4px'ten bilerek yükseltildi), küçük iç öğeler ve bırakma
alanları `{rounded.sm}`. Rozet ve chip tam yuvarlaktır (`q-badge` varsayılanı).

Kenarlık her zaman 1px ve `{colors.ayrac}` tonundadır. Izgara yüzeylerinde (ders programı,
atama ızgarası) hücre kenarlığı `border-collapse: collapse` ile tek çizgiye iner — çift
çizgi görünmez.

Bırakma hedefi (`drop-zone`) 2px **kesikli** kenarlıkla işaretlenir ve yalnız sürükleme
sırasında renklenir. Kesikli kenarlık bu sistemde tek anlama gelir: **buraya bırakabilirsin**.

### Named Rules

**Kesik Çizgi Kuralı.** Kesikli kenarlık yalnız bırakma hedefi içindir. Dekoratif ayraç,
placeholder çerçevesi veya boş-durum kutusu için kullanılmaz.

## Components

### Buttons
- **Shape:** Yumuşak köşe (`{rounded.md}`), Quasar varsayılanı 3px'ten yükseltildi.
- **Primary:** Mührü lacivert dolgu, beyaz metin, `unelevated` (gölgesiz), padding `6px 18px`.
- **Hover / Focus:** Zemin %12 koyulaşır; odak halkası 2px mührü lacivert, `outline-offset: -2px`.
  Odak halkası asla kaldırılmaz.
- **Cancel / Ghost:** `flat`, gri metin (`grey-7`), aynı padding. İptal her zaman solda,
  onaylayıcı eylem sağda.
- **Yıkıcı eylem:** Ret kırmızısı dolgu; yalnız gerçekten geri alınamayan işlemde
  (fesih, silme, kayıt sonlandırma).
- **İkon-yalnız buton:** `aria-label` + `<q-tooltip>` **zorunlu**. `title` attribute
  kullanılmaz. Dokunma hedefi en az 24×24 CSS px (WCAG 2.2 SC 2.5.8).

### Chips / Badges
- **Durum rozeti (`StatusBadge`):** Aşamayı taşıyan tek görsel dil. 11 ton, hepsi tema
  renginden türetilmiş, hepsi beyaz metinle ≥4,5:1 (en düşük ölçülen 4,69:1). Aynı aşamadaki
  durumlar aynı tonu paylaşır, aşamalar arası ayrışır: bekleyen (hardal) → ara aşama (teal)
  → bilgilendirici (cyan) → aktif (yeşil) → terminal başarı (koyu yeşil) → uyarı (koyu hardal)
  → olumsuz (kırmızı) → tamamlanmış (koyu mavi) → nötr/taslak (gri).
- **Kategori rozeti:** Anlamsal durum taşımayan etiket (sektör, rol, form tipi) `bg-neutral`
  kullanır — `positive`/`info` gibi bir role bağlanmaz, yanlış anlam yükler.
- **State:** Rozet her zaman metin etiketi taşır. Renk yalnız ikincil sinyaldir.

### Cards / Containers
- **Corner Style:** `{rounded.md}`
- **Background:** Kağıt beyazı; vurgulanan kart `-soft` zemin tonu
- **Shadow Strategy:** Yok — `flat bordered`. Bkz. Elevation & Depth
- **Border:** 1px `{colors.ayrac}`
- **Internal Padding:** `{spacing.md}`

### Inputs / Fields
- **Style:** `outlined` + `dense` varsayılan. Zemin kağıt beyazı, kenarlık `{colors.ayrac}`,
  yarıçap `{rounded.md}`, yükseklik ~40px.
- **Focus:** Kenarlık mührü laciverte döner ve kalınlaşır (Quasar `outlined` davranışı).
- **Arama girdisi:** `prepend` konumunda arama ikonu, temizlenebilir, 400ms debounce.
  Genişlik en az 250px.
- **Error / Disabled:** Hata ret kırmızısı kenarlık + altta hata metni. Kapalı dönemde tüm
  girdiler `disable` — gizlenmez, kilitli görünür.

### Navigation
- **Style:** Sol çekmece, `bordered`, kaydırma alanı içinde `q-list padding`. Grup başlıkları
  `q-expansion-item`, alt öğeler `inset-level: 1` + `dense`.
- **States:** Aktif grup başlığı mührü lacivert metin; aktif öğe Quasar `active` zemini;
  tümü `v-ripple`.
- **Üst bar:** Mührü lacivert zemin, beyaz metin. Solda menü düğmesi, ortada ürün adı,
  sağda kullanıcı adı → bildirim (okunmamış sayacı kırmızı `floating` rozet) → çıkış.
- **Mobil:** Çekmece `md` altında örtüşür ve varsayılan olarak kapalıdır.

### Data Table (`AppTable`) — imza bileşeni
Sistemin taşıyıcı yüzeyi. `flat bordered`, `binary-state-sort` açık.
- **Filtre çubuğu:** Üstte tek satır — filtreler solda, arama sağda (`q-space` ile itilir).
- **İlk yükleme:** Spinner değil, **44px yüksekliğinde 6 skeleton satır** — düzen kaymaz.
  Sonraki yüklemeler tablonun içinde `q-inner-loading` + dişli spinner.
- **Boş durum:** Nötr `inbox` ikonu (48px, gri) + "Kayıt bulunamadı" + isteğe bağlı eylem
  çağrısı slot'u. Uyarı ikonu **kullanılmaz** — boş liste hata değildir.
- **Sayfalama:** Sunucu taraflı, 10/20/50 seçeneği.

### Yan-panel Form (`FormDialog`) — imza bileşeni
Sağdan kayan side-sheet; `slide-left` girer, `slide-right` çıkar. Genişlik 480px varsayılan,
`sm` altında tam ekran. Üstte renkli toolbar (eylemin anlamına göre: onay yeşili, ret
kırmızısı, uyarı hardalı), ortada kendi içinde kayan içerik, altta ayraçla ayrılmış eylem
çubuğu (İptal solda `flat`, kaydet sağda dolgu).

### Durum Bildirimi (`AppNotice`)
Beş tür: `info`, `warning`, `error`, `success`, `readonly`. Her biri `-soft` zemin +
`-strong` metin + eşleşen ikon. `readonly` türü nötr gridir ve kilit ikonu taşır — kapalı
dönem uyarısının kalıcı yeridir.

### Ders Programı / Atama Izgarası (`ScheduleGrid`, `AssignmentGrid`)
Sistemin en yoğun yüzeyi. Hücre zeminleri tema türevi: boş `positive 14%`, atanmış
`info 12%`, dolu nötr gri. Sürükle-bırak hedefi 2px kesikli kenarlıkla işaretlenir,
sürükleme sırasında yeşile döner. Klavye seçimi 2px mührü lacivert `outline` ile gösterilir —
sürükle-bırak tek erişim yolu değildir.

## Do's and Don'ts

### Do:
- **Do** her satırda aşamayı ve bekleyen tarafı göster — `StatusBadge` ile, metin etiketiyle birlikte.
- **Do** yumuşak tonları `color-mix()` ile tema değişkeninden türet (`bg-positive-soft`,
  `text-warning-strong`) ve `color-mix()` desteklenmeyen tarayıcı için düz hex yedeği bırak.
- **Do** kiracı markasını küratörlü sekiz seçenekten bir **anahtarla** ayarla; hex tek yerde,
  palet kümesinde yaşar ve arayüzde ikinci kez tanımlanmaz.
- **Do** derinliği zemin tonuyla kur; gölgeyi yalnız dialog, menü, örtüşen çekmece ve üst bar
  için sakla.
- **Do** yalnız ikon içeren her butona `aria-label` + `<q-tooltip>` ver ve dokunma hedefini
  en az 24×24 CSS px yap.
- **Do** boş durumu nötr ikon + eylem çağrısıyla göster (`inbox`, "Kayıt bulunamadı").
- **Do** sayı gösteren her yüzeyde `tabular-nums` kullan.
- **Do** oluştur/düzenle formunu ayrı route sayfası yap; kısa aksiyonu `FormDialog` yan
  paneline koy.
- **Do** ilk yüklemede içerik-şekilli skeleton kullan (44px satır), spinner değil.
- **Do** Türkçe karakterleri tam yaz (ç ş ğ ü ö ı İ) ve MEB terminolojisini kullan
  ("1. Dönem" / "2. Dönem").

### Don't:
- **Don't** ham Quasar palet tonu yazma (`bg-orange-1`, `text-blue-8`, `text-orange-9`).
  Tema dışına düşer ve kiracı rengi değiştiğinde yerinde donar. **Mevcut 13 nokta borçtur,
  çoğaltılmaz.**
- **Don't** kiracıya serbest renk girdirme ve veritabanına ham hex yazma — ölçülmemiş bir
  primary üst bardaki, birincil butondaki ve rozetlerdeki beyaz metni okunmaz yapar; bunu
  hiçbir derleyici göremez.
- **Don't** Resmî Hardal'ı metin rengi olarak kullanma (beyaz üzerinde 2,4:1) ve "sıra sizde"
  dışında bir anlama bağlama.
- **Don't** dinlenen bir karta veya tabloya `box-shadow` verme. `flat bordered` varsayılandır.
- **Don't** merkezî modal açma — bu sistemde form ya ayrı sayfadır ya sağdan kayan panel.
- **Don't** boş listeye uyarı (⚠) ikonu koyma; boş durum bir hata değildir.
- **Don't** gövde puntosunu 14px'ten büyütme — 38 sayfalık tablo düzeni kayar.
- **Don't** `title` attribute'ünü tooltip yerine kullanma (WCAG için güvenilir değil).
- **Don't** kesikli kenarlığı bırakma hedefi dışında kullanma.
- **Don't** rengi tek sinyal yapma — her durum rozeti metin etiketi taşır.
- **Don't** yeni bir renk çiftini kontrast ölçmeden ekleme (metin eşiği 4,5:1).
- **Don't** Quasar gri tonunu ezberden yazma; ölçek Material'dan bir basamak kayıktır. `grey-6`
  (`#9e9e9e`, 2,68:1) ve `grey-5` (`#bdbdbd`, 1,88:1) ne metin ne de beyaz metinli rozet zemini
  olabilir — metin tabanı `grey-7`, hex `variables.sass`tan doğrulanır.
- **Don't** koyu tema değişkenlerini (`{colors.koyu-yuzey}`, `{colors.koyu-sayfa}`) canlı
  kabul etme — tanımlıdır ama ürün kararı değildir.
