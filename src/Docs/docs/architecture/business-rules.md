---
title: İş Kuralları
---

# MESNET — İş Kuralları

Bu doküman, MESNET sistemindeki iş kurallarını mevzuat referanslarıyla birlikte tanımlar.

## Mevzuat Referansları

| Mevzuat | Numara | Konu |
| ------- | ------ | ---- |
| Mesleki Eğitim Kanunu | 3308 sayılı (19.06.1986) | Çırak, kalfa, usta eğitimi ve işletmelerde mesleki eğitim |
| Mesleki Eğitim Yönetmeliği | 18812 sayılı (07.09.2013) | Uygulama esasları ve detay kurallar |
| MEB Ortaöğretim Kurumları Yönetmeliği | 28758 sayılı (07.09.2013) | Ortaöğretim eğitim-öğretim, devamsızlık, staj, çalışma takvimi |
| Ulusal Bayram ve Genel Tatiller Hakkında Kanun | 2429 sayılı (17.03.1981) | Resmi tatil günleri tanımı |

---

## 1. İşletme Kayıt ve Onay Kuralları

### 1.1 Kayıt Yöntemleri

- **Kurum tarafından kayıt:** Kurum yönetimi işletmeyi sisteme kaydeder. İşletme doğrudan **AKTİF** durumda oluşturulur.
- **İşletme kendi kaydı:** İşletme yetkilisi kendi kaydını açar. İşletme **ONAY_BEKLİYOR** durumunda oluşturulur. Kurum yönetimi onaylamadan işletme sisteme üye olamaz.

### 1.2 Onay Süreci

1. İşletme yetkilisi kayıt formunu doldurur
2. Sistem `BusinessApprovalRequested` event'i yayınlar
3. Kurum yönetimi bilgilendirilir
4. Kurum onaylarsa → `BusinessApproved` → durum **AKTİF**
5. Kurum reddederse → `BusinessRejected` → durum **REDDEDİLDİ**

### 1.3 Gerekli Belgeler

- Ustalık belgesi veya usta öğreticilik belgesi (3308 Madde 15)
- İşyeri açma izni
- Ticaret/esnaf sicil kaydı

---

## 2. İşletme Durum Yönetimi

### 2.1 Durum Geçişleri

```
ONAY_BEKLİYOR → AKTİF (kurum onayı)
ONAY_BEKLİYOR → REDDEDİLDİ (kurum reddi)
AKTİF → PASİF (geçici devre dışı)
PASİF → AKTİF (yeniden aktifleştirme)
AKTİF → KAPATILMIŞ (kalıcı kapatma)
PASİF → KAPATILMIŞ (kalıcı kapatma)
```

### 2.2 Event Sourcing Garantisi

- Tüm durum geçişleri event sourcing ile saklanır
- Geçmiş veriler hiçbir durumda kaybolmaz
- Kapatılmış işletmelerin geçmiş staj, devamsızlık ve ödeme kayıtları erişilebilir kalır

### 2.3 Kısıtlamalar

- Aktif stajyeri olan işletme doğrudan **KAPATILMIŞ** yapılamaz; önce stajyerlerin transferi gerekir
- **PASİF** işletmeye yeni stajyer atanamaz
- **KAPATILMIŞ** işletme yeniden aktifleştirilemez

---

## 3. Kontenjan Kuralları

- İşletme, alabileceği stajyer öğrenci kontenjanını belirleyebilir
- Kontenjan doluyken yeni stajyer ataması yapılamaz
- Kontenjan değişikliği `BusinessCapacityChanged` event'i ile takip edilir
- 3308 Madde 18: 10+ personel çalıştıran işletmeler personel sayısının **%5'inden az olmamak üzere** stajyer almak zorundadır

---

## 4. Fesih Kuralları

### 4.1 Fesih Talep Edebilecekler

- **İşletme yetkilisi:** Gerekçeli fesih talebi yapabilir (zorunlu gerekçe alanı)
- **Kurum yönetimi:** Disiplin, sağlık, devamsızlık aşımı gibi nedenlerle fesih başlatabilir
- **Sistem (otomatik):** Devamsızlık limiti aşıldığında `AttendanceLimitExceeded` event'i ile tetiklenir

### 4.2 Fesih Nedenleri

| Kod | Neden | Açıklama |
| --- | ----- | -------- |
| DISIPLIN | Disiplin | Öğrencinin disiplin kurallarına aykırı davranışı |
| SAGLIK | Sağlık | Öğrencinin sağlık durumunun staja engel olması |
| DEVAMSIZLIK_ASIMI | Devamsızlık Aşımı | Devamsızlık limitinin aşılması (otomatik tetikleme) |
| ISLETME_TALEBI | İşletme Talebi | İşletmenin gerekçeli fesih talebi |
| OGRENCI_TALEBI | Öğrenci Talebi | Öğrenci veya velisinin fesih talebi |
| DIGER | Diğer | Yukarıdaki kategorilere girmeyen durumlar |

### 4.3 Dijital Onay Zinciri

Fesih kararı şu sırayla dijital onay gerektirir:

1. **Öğrenci velisi** (18 yaş altı öğrenciler için zorunlu)
2. **Koordinatör öğretmen**
3. **Müdür yardımcısı**
4. **Müdür**
5. **İşletme yetkilisi**

**Takılma Kuralı:** Onay zinciri takılırsa (veli veya öğretmen onaylamıyorsa), yetkili müdür yardımcısı override yetkisiyle formu onaylayıp ıslak imzaya gönderebilir.

### 4.4 Islak İmza Süreci

1. Müdür yardımcısı dijital onay tamamlandığında veya override kullandığında fesih formunu üretir
2. QuestPDF ile ıslak imza alanları bulunan PDF form oluşturulur
3. Form yazdırılır ve tüm tarafların ıslak imzası alınır
4. İmzalı form taranarak sisteme yüklenir
5. Okul yönetimi tüm süreci izleyebilir

### 4.5 Fesih Sonrası

- Feshedilen stajyerin yeni işletmeye yerleştirilmesi Internship saga tarafından koordine edilir
- `InternshipReplacementRequested` event'i → Enrollment modülü yeni eşleştirme yapar

---

## 5. Devamsızlık Kuralları

**Yasal Dayanak:** MEB Ortaöğretim Kurumları Yönetmeliği Madde 35-36, 3308 Sayılı Kanun

### 5.1 Devamsızlık Girişi Yapılabilecek Günler

İşletme yetkilisi **sadece aşağıdaki günler HARİCİNDE** günlük devamsızlık girişi yapabilir:

- **Resmi tatil günleri** — 2429 sayılı Kanun'a göre belirlenen ulusal bayram ve genel tatiller
- **Hafta sonu tatili** — Cumartesi ve Pazar günleri
- **Yarıyıl tatili** — Bakanlıkça belirlenen dönem arası tatil
- **Ara tatiller** — Her dönemde bir kez yapılan ara tatil
- **Yaz tatili** — Ders kesiminden sonraki dönem (staj dönemi hariç)

Sistem, devamsızlık girişi yapılmak istenen tarihi `WorkCalendar` ile kontrol eder ve kısıtlı günlerde girişi engeller.

**Resmi Tatil Günleri (2429 sayılı Kanun):**

- 29 Ekim — Cumhuriyet Bayramı (28 Ekim saat 13:00'ten itibaren 1,5 gün)
- 23 Nisan — Ulusal Egemenlik ve Çocuk Bayramı (1 gün)
- 19 Mayıs — Atatürk'ü Anma, Gençlik ve Spor Bayramı (1 gün)
- 30 Ağustos — Zafer Bayramı (1 gün)
- 1 Ocak — Yılbaşı (1 gün)
- 1 Mayıs — Emek ve Dayanışma Günü (1 gün)
- 15 Temmuz — Demokrasi ve Milli Birlik Günü (1 gün)
- Ramazan Bayramı (3,5 gün — arife günü saat 13:00'ten itibaren)
- Kurban Bayramı (4,5 gün — arife günü saat 13:00'ten itibaren)

### 5.2 Devamsızlık Türleri

| Tür | Açıklama | Maaş Etkisi |
| --- | -------- | ----------- |
| MAZERETLİ | Geçerli mazereti olan devamsızlık | Kesinti yapılmaz |
| MAZERETSİZ | Geçerli mazereti olmayan devamsızlık | Ücret kesilir |
| SAĞLIK_RAPORU | Sağlık raporu ile belgelenen devamsızlık | Kesinti yapılmaz |

### 5.3 Devamsızlık Limiti ve Yaptırımlar

**Yasal Dayanak:** MEB Ortaöğretim Kurumları Yönetmeliği Madde 36

- Kurum yönetimi tarafından belirlenen devamsızlık limiti aşıldığında `AttendanceLimitExceeded` event'i tetiklenir
- Bu event Internship saga tarafından yakalanır ve otomatik fesih süreci başlatılır
- Özürsüz devamsızlık limiti: Kurum tarafından belirlenir (genellikle toplam iş günlerinin belirli bir yüzdesi)
- Sağlık raporu ile belgelenen devamsızlıklar özürsüz devamsızlığa dahil edilmez

### 5.4 İşletmede Mesleki Eğitim Süreleri

**Yasal Dayanak:** MEB Ortaöğretim Kurumları Yönetmeliği Madde 9/3-4

- İşletmelerde yapılan mesleki eğitimde bir ders saati **60 dakikadır** (okulda 40 dk)
- Staj süresi de **60 dakika** üzerinden değerlendirilir
- İşletmelerdeki mesleki eğitim **gündüz yapılması esastır**
- Sanayi dışı sektörlerde (turizm, sağlık vb.) il istihdam kurulu kararıyla gece eğitim yapılabilir:
  - Günde **8 saati** geçemez
  - Saat **22:00'yi** geçemez
- Yoğunlaştırılmış eğitimde haftalık azami çalışma saatini geçmemek şartıyla **haftada 6 gün** planlanabilir (veli/öğrenci isteği gerekli)

### 5.5 Devamsızlık Girişi Yetkilendirme Kuralları

#### Öğrenci-İşletme Bağlantısı

Devamsızlık kaydı oluşturulurken öğrenci ve işletme bağımsız olarak seçilemez.
Her devamsızlık kaydı, aktif `InternshipPlacement`'taki öğrenci-işletme eşleşmesine dayanır.
Öğrenci seçildiğinde işletme bilgisi otomatik olarak doldurulur.

#### Rol Bazlı Görünürlük

| Rol | Görebildiği Öğrenciler | Açıklama |
|-----|----------------------|----------|
| Kurum Müdürü (InstitutionManager) | Kendi kurumundaki TÜM öğrenciler | InstitutionId eşleşmesi |
| Kurum Personeli (InstitutionStaff) | Kendi kurumundaki TÜM öğrenciler | InstitutionId eşleşmesi |
| Koordinatör Öğretmen (Teacher) | SADECE koordine ettiği öğrenciler | InternshipPlacement.TeacherId eşleşmesi |
| İşletme Yöneticisi (CompanyManager) | SADECE kendi işletmesindeki öğrenciler | InternshipPlacement.BusinessId eşleşmesi |

#### Onay Akışı

- **İşletme yöneticisi** devamsızlık girdiğinde → `Pending` (Onay Bekliyor) durumunda oluşur → Koordinatör öğretmene SSE bildirim gönderilir → onaylarsa `Recorded`
- **Koordinatör öğretmen** devamsızlık girdiğinde → doğrudan `Recorded` (onay gerekmez)
- **Kurum müdürü/personeli** devamsızlık girdiğinde → doğrudan `Recorded`

#### Backend Doğrulama Zinciri

1. Akademik dönem aktif mi? (`AcademicPeriodView`)
2. Öğrenci-işletme eşleşmesi mevcut mu? (`InternshipPlacementView`)
3. Tarih geçerli hafta içinde mi? (5.6 kuralı)
4. Tarih kısıtlı bir gün mü? (`WorkCalendar`)
5. Devamsızlık türü geçerli mi? (`AbsenceType` SmartEnum)

### 5.6 Devamsızlık Girişi Zaman Kısıtı

**MEB e-Okul Uyumu:** MEB, e-Okul sistemine devamsızlık girişi için **1 hafta** süre tanımaktadır. MESNET aynı kuralı uygular.

- Devamsızlık girişi **sadece geçerli hafta** (Pazartesi 00:00 – Pazar 23:59 UTC) için yapılabilir
- Geriye dönük (geçmiş hafta) veya ileriye dönük (gelecek hafta) devamsızlık girişi **engellenir**
- Backend'de `MarkAttendanceHandler` tarih kontrolü yapar: geçerli hafta dışındaki tarihler `DomainException` ile reddedilir
- Onay işlemi (`ApproveAttendance`) için de aynı hafta kısıtı geçerlidir

**Hata kodu:** `ATTENDANCE_OUTSIDE_CURRENT_WEEK`

### 5.7 Otomatik Onay ve Uyarı Mekanizması

İşletme tarafından girilen ancak koordinatör öğretmen tarafından **7 gün içinde onaylanmamış** devamsızlık kayıtları (`Pending` durumunda) için:

1. **7. günün başlangıcında** (UTC 00:00) kayıt otomatik olarak `Recorded` durumuna geçirilir
2. İlgili **müdür yardımcısına** bildirim gönderilir: "X öğrencisinin Y tarihli devamsızlığı otomatik onaylandı — koordinatör öğretmen tarafından zamanında onaylanmadı"
3. Geç bildirim durumu kayıt altına alınır

**Teknik:** Wolverine durable scheduled messaging ile günlük çalışan `AutoApproveExpiredAttendance` komutu

---

## 6. Maaş Hesaplama Kuralları

**Yasal Dayanak:** 3308 Sayılı Kanun Madde 25

### 6.1 Taban Ücret Hesabı

| Durum | Oran | Formül |
| ----- | ---- | ------ |
| Aday çırak / çırak | %30 | **Yaşına uygun** asgari ücret × 0.30 |
| 20+ personel çalıştıran işletmede öğrenci | %30 | Yaşına uygun asgari ücret × 0.30 |
| 20'den az personel çalıştıran işletmede öğrenci | %15 | Yaşına uygun asgari ücret × 0.15 |
| MEM 12. sınıf (kalfalık yeterliği) | %50 | Yaşına uygun asgari ücret × 0.50 |

> **Yaşına uygun asgari ücret** (Madde 25; MEB Ortaöğretim Kurumları Yönetmeliği 6/a): 16
> yaşından küçükler için ayrı (daha düşük) asgari ücret belirlenir. Sistemde
> `SalaryCalculationConfig.MinimumWageUnder16` alanı tutulur; tanımlı değilse yaş ayrımı
> yapılmaz ve genel asgari ücret uygulanır. Yaş, maaşın hesaplandığı aya göre bulunur.

> **Aday çırak / çırak** oranı işletme büyüklüğünden **bağımsızdır**: Madde 25 "aday çırak ve
> çırağa yaşına uygun asgari ücretin yüzde otuzundan ... aşağı ücret ödenemez" der. Kategori
> öğrenci kaydında seçilir (`Öğrenci` / `Aday Çırak` / `Çırak`).

> **Not:** Bu oranlar yasal asgari değerlerdir. Madde 25'in ilk cümlesi ücretin **sözleşmeyle**
> tespit edileceğini söyler; yüzdeler yalnız alt sınırdır. Sistem sözleşmedeki aylık ücreti
> (`Contract.AgreedMonthlyWage`) kaydeder ve yasal tabandan yüksekse onu esas alır; düşükse
> tabanı öder. Devlet katkısı matrahı **yasal taban** olarak kalır (Geçici Madde 12:
> "ödenebilecek en az ücret"), sözleşmedeki fazlası işveren payına eklenir.

> **%50 oranının şartı:** Kanun "**kalfalık yeterliğini kazanan** mesleki eğitim merkezi
> 12'nci sınıf öğrencileri" diyor. Yeterliği olmayan MESEM 12. sınıf öğrencisi işletme
> büyüklüğü oranına (%15/%30) tabidir. Bilgi eksikse düşük oran uygulanır.

> **Personel sayısı tanımı** (3308 Madde 24, son fıkra): "görev ve çalışma statüsüne
> bakılmaksızın işyerinde **1475 sayılı İş Kanununa tabi olarak çalıştırılan** personel sayısı".
> Stajyer ve çıraklar bu sayıya dâhil değildir — 4857 Madde 4/f çırakları İş Kanunu kapsamı
> dışında bırakır. Sayı 20 eşiğini geçtiğinde öğrenci ücreti ikiye katlandığı için tanım
> kritiktir.

### 6.2 Devamsızlık Kesintisi

Özürsüz devamsızlık ve ücretsiz izin günlerinde ücret kesilir.

**Formül:**

```
GünlükÜcret     = AylıkTabanÜcret / 30
OranlıTabanÜcret = GünlükÜcret × İstihdamGünSayısı      (bkz. §6.2.1)
KesintiBedeli   = GünlükÜcret × KesintiyeTabiGünSayısı
ÖdenecekÜcret   = OranlıTabanÜcret - KesintiBedeli
```

Kesinti **oranlanmış** tutardan düşer ve onu aşamaz — yarım ay çalışan öğrencinin ücreti
negatife düşemez.

### 6.2.1 İstihdam Günü Oranlaması (kısmi ay)

Öğrenci ay ortasında işletme değiştirebilir (fesih → yeni sözleşme; doğrudan transfer yoktur).
Bu durumda ücret ve devlet katkısı **her işletmede çalışılan gün oranında bölüşülür**:
ayrılınan işletme fesih gününe kadar, yeni işletme sözleşme tarihinden ay sonuna kadar.

**Gün sayımı — SGK usulü 30 günlük ay:**

| Durum | İstihdam günü |
| --- | --- |
| Ay tam çalışıldı | **30** (ayın gün sayısına bakılmaz — Şubat da 30, Temmuz da 30) |
| Ay eksik çalışıldı | **Fiilî gün** (iki uç dahil) |
| Sözleşme ayla hiç kesişmiyor | 0 — maaş dönemi açılmaz |

Devlet katkısı **aynı oranla** hesaplanır (§6.3'teki matrah oranlanır).

> **Kabul edilen sonuç:** 31 günlük ayda bölüşme olduğunda gün toplamı 31 olur ve ödenen toplam
> tabanı aşar (31/30). Kırpma yapılmaz; her işveren kendi istihdam günü için sabit günlük ücreti
> öder. Kırpmanın hangi işletmeden düşeceği keyfî bir karar gerektirirdi.

Maaş dönemi kimliği bu yüzden **(sözleşme, ay)** ikilisinden türetilir — (öğrenci, ay) olsaydı
ayda tek dönem açılabilir ve iki işverenin yükümlülüğü tek kayda sıkışırdı.

Karar kaydı ve gerekçe: issue #154.

**Kesintiye tabi devamsızlık türleri** (`AbsenceType.AffectsSalary`):

| Tür | Kesinti |
| --- | ------- |
| Mazeretsiz (`Unexcused`) | **Kesilir** |
| Ücretsiz İzin (`UnpaidLeave`) | **Kesilir** |
| Mazeretli (`Excused`) | Kesilmez |
| Sağlık Raporu (`HealthReport`) | Kesilmez |
| Ücretli İzin (`PaidLeave`) | Kesilmez |

> Ücretli izin, MEB Ortaöğretim Kurumları Yönetmeliği'nde işletmenin yükümlülüğü olarak
> tanımlıdır: telafi eğitimi ve okuldaki sınav günleri için, ayrıca ara tatil/yarıyıl/yaz
> tatilinde toplam bir ay.

> Yalnız **onaylanmış** kayıtlar sayılır — işletmenin girdiği ve henüz öğretmence onaylanmamış
> (`Pending`) devamsızlık öğrencinin ücretini kesmez.

**Türü seçen taraf, dolaylı olarak kesintiyi seçer (#175).** Bu yüzden tür girişinde iki kural
vardır:

1. **İşletme resmî izin veremez, yalnız devamsızlık bildirir.** İşletme tarafı (hüküm izni
   `attendance:direct-entry` olmayan kullanıcı) yalnız **Mazeretsiz** girebilir. Mazeret, izin
   ve sağlık raporu birer *sınıflandırma kararıdır* ve okul tarafındadır: mazeret veli
   dilekçesiyle öğrenci işlerinde çözülür, sağlık raporu kendi onay zincirinden geçer (#172),
   tür değişikliği `/correct` ile yapılır.
2. **Ücretli izin hakkı yalnız MESEM'dedir.** Örgün eğitimde ücretli izin hakkı yoktur; o
   günler için sağlık raporu ya da veli izni gerekir. Öğrencinin eğitim türü bilinmiyorsa
   ücretli izin **reddedilir** — eksik veri sessizce para sonucu doğurmasın.

**Sağlık raporu kesintiyi ancak ONAYLANDIĞINDA kaldırır (#172).** Rapor girişi bilinçli olarak
geniştir: işletme yetkilisi, işletme İK, usta öğretici ve öğrenci de yükleyebilir. Ama yükleme
tek başına devamsızlık türünü değiştirmez — koordinatör öğretmen onaylayana kadar tür ne ise
kesinti ona göre işler. Aksi hâlde **ödemeyi yapan taraf kendi kesintisini tek taraflı
kaldırabilirdi**.

| Kim yükledi | Sonuç |
|---|---|
| İşletme yetkilisi, işletme İK, usta öğretici, öğrenci | `Pending` — tür değişmez, kesinti sürer |
| Koordinatör öğretmen, müdür yardımcısı, müdür | Doğrudan geçerli — tür `HealthReport`, kesinti kalkar |

Reddedilen rapor türü değiştirmez; kesinti aynen uygulanır. 2. adımda müdür yardımcısı / müdür
kesinti kararını mevcut dekont onay zincirinde uygular. Ayrıntı:
[İzin Matrisi → Sağlık Raporu Onay Zinciri](../actors/permissions.md).

**Örnek:** 20+ personel işletmede, asgari ücret 22.104,67 TL ise:
- Taban ücret = 22.104,67 × 0.30 = 6.631,40 TL
- 3 gün mazeretsiz devamsızlık → Kesinti = (6.631,40 / 30) × 3 = 663,14 TL
- Ödenecek = 6.631,40 - 663,14 = 5.968,26 TL

### 6.3 Devlet Katkısı

**Yasal Dayanak:** 3308 Geçici Madde 12

| Öğrenci Tipi | İşletme Büyüklüğü | Devlet Katkısı Oranı |
| ------------ | ------------------ | -------------------- |
| MEM dışı okul öğrencisi | <20 personel | Ücretin 2/3'ü |
| MEM dışı okul öğrencisi | ≥20 personel | Ücretin 1/3'ü |
| MEM öğrencisi | Tüm işletmeler | Ücretin tamamı |

**Hesaplama:**

```
DevletKatkısı = AylıkTabanÜcret × DevletKatkısıOranı
İşverenPayı   = ÖdenecekÜcret - DevletKatkısı
```

> **Matrah taban ücrettir, ödenecek ücret değil.** Geçici Madde 12 katkıyı
> "**ödenebilecek en az ücretin**" oranı olarak tanımlıyor — yani §6.1'deki yasal taban.
> Devamsızlık kesintisi taban ücreti değil ödenecek ücreti düşürür; dolayısıyla kesinti
> devlet katkısını azaltmaz, işveren payını azaltır.
>
> Oranlar tam kesirdir: `1/3` ve `2/3`. Yaklaşık değer (`0,3333` / `0,6667`) kullanılmaz.
>
> Kesintinin katkı matrahını düşürüp düşürmediği kanunda açık değil (Geçici Madde 12:
> "usul ve esaslar Bakanlık ve Türkiye İş Kurumu tarafından belirlenir"). Uygulamada katkı,
> fiilen ödenen ücretle sınırlandırılır — aksi halde işveren payı negatife düşerdi.

**Katkının ödenmediği hâller:**

| Hâl | Katkı | Ücret |
| --- | --- | --- |
| Kamu kurum/kuruluşu | **0** — Geçici Madde 12: "Kamu kurum ve kuruluşlarına Devlet katkısı ödenmez" | Değişmez |
| Sınıf tekrarı (§6.3.1) | **0** | Değişmez |

### 6.3.0 Okulda Staj — Ne Ücret Ne Katkı

Staj yeri bulunamayan öğrenci stajını **okulda** yapar. Bu hâlde **ücret de devlet katkısı da
ödenmez**; 3308 ikisini **ayrı ayrı** kapsam dışı tutuyor:

| Kaynak | Kapsam dışı bıraktığı |
| --- | --- |
| **Madde 25**, ücret tabanı fıkrasının son cümlesi | İşletmenin ödeyeceği **ücret** |
| **Geçici Madde 12**, aynı cümle | **Devlet katkısı** |

> *"Staj yapacak işletme bulunamaması nedeniyle stajını okulda yapan ortaöğretim öğrencileri
> ... bu fıkra hükmü kapsamı dışındadır."*

**Temsil:** yerleştirmenin `BusinessId` alanı **null**, `PlacementType` = `School`. Kamu kurumu
işareti (§6.3, `IsPublicInstitution`) bu hâlin çözümü **değildir** — o yalnız katkıyı sıfırlar,
ücret yükümlülüğünü bırakır (dekont beklenir, gecikme uyarısı gider).

**Sonuçları:**

- Sözleşme kurulmaz → maaş dönemi açılmaz (dönemler sözleşmeden doğar, §6.2.1) → dekont ve
  gecikme uyarısı doğmaz
- Koordinasyon saati doğmaz: koordinasyon satırı (işletme, alan, dönem) üçlüsünden üretilir,
  işletme olmadığı için satır hiç oluşmaz
- **Staj sürer:** devamsızlık takibi, dönem notu ve mezuniyet akışı işlemeye devam eder
- Öğrenciye **gözetmen** (alan ya da atölye şefi) atanabilir; bu atama **ücret doğurmaz**
- İşletme başına üretilen raporlara (aylık devamsızlık, toplu belge) girmez

Karar kaydı: issue #159.

#### Dönem notunu okul girer, fiş üretilmez (#171)

Dönem notu akışının her adımı işletmeye bağlıydı: uç `company:grade:enter` istiyor, işletme
kimliği `business_id` claim'inden okunuyor, öğrenci listesi işletme kapsamlı görünümden
geliyordu. Okulda staj yerleştirmesi o görünüme hiç girmediği için öğrenci not giriş ekranında
**görünmüyor ve notu hiç girilemiyordu** — belirti ancak dönem sonunda "bu öğrencinin notu
nerede" diye sorulduğunda çıkardı.

| | İşletmede staj | Okulda staj |
| --- | --- | --- |
| Notu giren | İşletme yetkilisi | **Alan şefi, müdür yardımcısı, müdür** |
| İzin | `company:grade:enter` | `institution:school-grade:enter` |
| Kapsam | `business_id` claim'i | `institution_id` claim'i + okulda staj yerleştirmesi |
| `StudentTermGrade.BusinessId` | Dolu | **null** |
| Usta öğretici adı | Girilir | Alan yok (işletme kavramı) |
| MEB Form 8 (Dönem Not Fişi) | Üretilir | **Üretilmez** |

**Fiş neden üretilmez:** form "İşletmenin Adı" alanı ve iki işletme imzası (usta öğretici,
işletme yetkilisi) taşır. Sahibin ifadesi: *"Okulda staj için ayrı form yok, hatta form yok
genel olarak."* Not kaydı yalnız öğrencinin başarı değerlendirmesi için tutulur.

Üretim yolu **üç katmanda** kapalıdır: okul gönderimi `StudentTermGradeSubmitted` olayını
yayınlamaz (Reporting kayıttan haberdar olmaz), fiş listesi (`GET /submitted`) işletmesiz
notları filtreler, ve fiş üretim handler'ı işverensiz yerleştirmeyi açık hatayla reddeder.

**İki akış birbirinin üstüne yazamaz:** işletme ucu okulda staj notunu, okul ucu işletme notunu
düzenleyemez/gönderemez — her iki yönde de ayrı hata döner.

> **Önek notu (ADR-0001):** izin `institution:` öneklidir — okulda staj yapıldığında kurum
> işverenin yerine geçer, bu kurumun işidir. Önek **kapsamı belirlemez**: "herkes kendi
> kurumuna göre yetkilenir" kuralı `institution_id` claim'inden gelir ve izinden bağımsızdır.
> `institution:*` yalnız müdürdedir; müdür yardımcısı ve alan şefi izni **açık satırla** alır.

Karar kaydı: issue #171.

### 6.3.1 Sınıf Yılı Başına Tek Katkı

> Bir öğrenci belirli bir **sınıf yılı** için devlet katkısını **bir kez** alır. O sınıf yılı
> tekrar edildiğinde katkı hesaplanmaz. Öğrenci katkı alınmamış bir sınıf yılına **terfi
> ettiğinde** katkı yeniden işler.

**Örnek:** 11. sınıfta kalan öğrenci, 11'i tekrar okuduğu yıl boyunca katkı almaz. 12. sınıfa
geçtiğinde katkı yeniden başlar — 12 için henüz katkı alınmamıştır.

- Kural **tüm öğrenciler** için geçerlidir, yalnız MESEM için değil
- **Ücret etkilenmez.** Katkı işletmeye ödenir; öğrenci parasını işletmeden alır. Bloke, öğrencinin
  ücretini değil **işveren payını** (`ÖdenecekÜcret − DevletKatkısı`) yükseltir. MESEM'de katkı en
  az ücretin tamamı olduğu için işletmenin maliyeti sıfırdan tam ücrete çıkar
- Katkı **fiilen ödendiğinde** (onay zinciri tamamlandığında) kaydedilir; reddedilen ödeme bloke
  üretmez
- Kayıt, katkının ilk alındığı **akademik dönemi** de tutar. Aynı dönemin sonraki ayları normal
  işler; bloke yalnız sonraki bir dönemde aynı sınıf yılı görülünce doğar. Bu ayrım olmadan
  öğrenci ilk yılının ikinci ayından itibaren katkısını kaybederdi
- Katkısı bloke öğrenci **sözleşme kurulurken** uyarı olarak gösterilir — işletme maliyeti ayın
  sonunda dekont gelirken öğrenmemelidir

> **Mevzuat notu:** 3308 metninde sınıf tekrarına dair açık bir hüküm **yoktur**; Geçici Madde 12
> usul ve esasları Bakanlık ve İŞKUR'a bırakıyor. Kural alan bilgisine dayanıyor — MEB genelgesi
> veya İŞKUR usul ve esaslarından teyit alınması önerilir. Tasarım teyitten bağımsız olarak
> doğrudur; teyit yalnız eşiğin yerini değiştirebilir.

Karar kaydı ve gerekçe: issue #161.

**İstisnalar:**

- Kamu kurum ve kuruluşlarına devlet katkısı **ödenmez**
- Stajını okulda yapan öğrenciler devlet katkısı kapsamı **dışındadır**
- Her öğrenci devlet katkısından **bir defaya mahsus** yararlanır

### 6.4 Vergi Muafiyeti

3308 Madde 25, fıkra 3: "Aday çırak, çırak ve öğrencilere ödenecek ücretler **her türlü vergiden müstesnadır**."

### 6.5 SGK Primleri

3308 Madde 25, fıkra 4: Sigorta primleri **asgari ücretin %50'si** üzerinden hesaplanır ve **Bakanlık/üniversite bütçesinden** karşılanır. İşletme SGK primi ödemez.

### 6.6 Ödeme Takvimi

- İşletme her ayın **8'ine kadar** öğrenci banka hesabına ücret yatırır
- Dekont her ayın **25'ine kadar** okula teslim edilir

**Dekont onay zinciri** (sıra zorunludur, atlanırsa HTTP 422):

| # | Aktör | İşlem | Faz (`PaymentPhase`) |
|---|-------|-------|----------------------|
| 1 | İşletme | Dekontu yükler | `ReceiptUploaded` |
| 2 | **Öğrenci** | Parayı hesabına aldığını onaylar | `StudentConfirmed` |
| 3 | Koordinatör öğretmen | Dekontu onaylar | `TeacherApproved` |
| 4 | Müdür yardımcısı | Son onayı verir | `DeputyApproved` → `Completed` |

Öğrenci onayı **ilk sırada** çünkü dekont, ödemenin *yapılmış olduğunun* belgesidir; paranın
gerçekten hesaba geçtiğini doğrulayabilecek tek taraf öğrencidir. Okul tarafı (öğretmen, müdür
yardımcısı) bu doğrulamadan önce onay verirse, kimsenin teyit etmediği bir ödemeyi onaylamış olur.

Sıra saga (`PaymentSaga`) içindeki guard'larla ve üç handler'daki faz kontrolüyle zorlanır:
`ConfirmSalaryHandler`, `ApproveReceiptByTeacherHandler`, `ApproveReceiptByDeputyHandler`.

> Bu tabloda "müdür yardımcısı" denen aktör, eski metinde "Kurum" olarak geçiyordu — kodda
> karşılığı `ReceiptApprovedByDeputy` / `PaymentPhase.DeputyApproved`'dır (#81).

---

## 7. İşletme Yükümlülükleri

**Yasal Dayanak:** 3308 Sayılı Kanun Madde 18, 15, 21

### 7.1 Stajyer Alma Zorunluluğu

| Personel Sayısı | Zorunluluk |
| --------------- | ---------- |
| 10+ personel | Personel sayısının **%5'inden az olmamak üzere** stajyer almak zorunda |
| 10'dan az personel | Stajyer alabilir ama zorunlu değil |

> **Not:** 3308 Madde 18, son fıkra: "Bu maddede belirtilen on personel sayısını beş personele kadar indirmeye Cumhurbaşkanı yetkilidir."

### 7.2 Usta Öğretici Zorunluluğu

3308 Madde 15: Stajyer almak için işyerinde **usta öğretici bulunması şarttır**.

Usta öğretici gereksinimleri:
- Ustalık yeterliğini kazanmış
- İş pedagojisi eğitimi almış
- Usta öğreticilik belgesi sahibi

### 7.3 Eğitim Birimi Kurma Zorunluluğu

3308 Madde 18: 10 ve daha fazla öğrenciye beceri eğitimi yaptıracak işletmeler **eğitim birimi** kurmak zorundadır.

### 7.4 Mesleki Eğitim Katılma Payı

3308 Madde 24: 10+ personel çalıştıran ve beceri eğitimi **yaptırmayan** işletmeler, her öğrenci için aylık katılma payı öder:

| Personel Sayısı | Ödeme Oranı |
| --------------- | ----------- |
| 10-19 personel | Asgari ücretin net tutarının 1/3'ü |
| 20+ personel | Asgari ücretin net tutarının 2/3'ü |

### 7.5 İşyeri Şartları

3308 Madde 21: Stajyer öğrenciler işyerinin şartlarına ve çalışma düzenine uymak zorundadır.

### 7.6 İzin Hakkı

3308 Madde 26: Stajyer öğrencilere her yıl tatil aylarında **bir ay ücretli izin** verilir. Mazereti kabul edilenlere okul müdürlüğünün görüşüyle **bir aya kadar ücretsiz izin** verilebilir.

---

## 8. Sözleşme Kuralları

### 8.1 Sözleşme Zorunluluğu

- Her staj için yazılı sözleşme zorunludur
- Sözleşme tarafları: Öğrenci (velisi), İşletme, Kurum
- Sözleşmede ücret ve artış oranı belirtilir

### 8.2 İmza Gereksinimleri

- Kurum yönetimi imzası
- İşletme yetkilisi imzası
- Öğrenci imzası (18 yaş altında veli imzası zorunlu)
- Tüm imzalar tamamlanmadan sözleşme aktifleştirilemez

### 8.3 Sözleşme Yaşam Döngüsü

```
TASLAK → İMZA_BEKLİYOR → AKTİF → ASKIDA → FESHEDİLMİŞ / TAMAMLANMIŞ
```

---

## 9. Çalışma Takvimi ve Ders Yılı Kuralları

**Yasal Dayanak:** MEB Ortaöğretim Kurumları Yönetmeliği Madde 14-15

### 9.1 Ders Yılı Süresi

- Ders yılı **iki döneme** ayrılır ve her dönemde **bir ara tatil** yapılır
- Ders yılının **180 iş gününden az olmaması** esastır
- 180\. iş günü hafta arasına rastlarsa haftanın son iş gününe uzatılır
- Ders yılının başlama, ara tatil, yarıyıl tatili, yaz tatili ve ders kesim tarihleri **Bakanlıkça** belirlenir
- Çalışma takvimi il millî eğitim müdürlüklerince hazırlanır, **valilik onayı** ile yürürlüğe girer

### 9.2 Öğretime Ara Verme

Aşağıdaki durumlarda il/ilçe hıfzıssıhha kurulu kararı ve mahalli mülki idare amiri onayıyla öğretime ara verilebilir:

- Olağanüstü durumlar
- Sel, deprem gibi doğal afetler
- Salgın hastalıklar
- Aşırı sıcak veya soğuk hava koşulları

> **Sistem Etkisi:** Öğretime ara verildiğinde işletmelerdeki beceri eğitimi gören öğrenciler de bu kapsamda değerlendirilir. WorkCalendar'a geçici kısıtlama eklenir.

### 9.3 Staj Takvimi ile İlişki

- İşletmelerde mesleki eğitim takvimi okul çalışma takvimi ile uyumlu olmalıdır
- 3308 sayılı Kanun'a göre sözleşmeler devam ettiği sürece öğrenciler ücretli/ücretsiz izin dışında işletmede eğitime devam eder
- Yoğunlaştırılmış eğitim programında veli/öğrenci isteğiyle haftada 6 gün planlanabilir

---

## 10. Yaş ve Kayıt Kuralları

**Yasal Dayanak:** MEB Ortaöğretim Kurumları Yönetmeliği Madde 21

### 10.1 Yaş Şartları

| Kurum | Yaş Şartı |
| ----- | --------- |
| Ortaöğretim kurumları | Öğretim yılı başında **18 yaşını bitirmemiş** olma |
| Mesleki eğitim merkezi | **14 yaşını doldurmuş** olma |
| Üst yaş sınırı | **22 yaşını tamamladığı** eğitim yılı sonunda mezun olamayan → ilişik kesilir |

> **Not:** Öğrenimine ara vermemiş olanlarda yaş şartı aranmaz.

### 10.2 Staj Sözleşmesine Etkisi

- 18 yaş altı öğrencilerde **veli imzası zorunludur** (sözleşme, fesih, izin işlemleri)
- Öğrenci yaşı sisteme kayıt sırasında hesaplanır ve ilgili iş akışlarını (onay zinciri vb.) etkiler

---

## 11. Koordinatör Öğretmen Görevlendirme ve Ek Ders Kuralları

**Yasal Dayanak:** MEB Ortaöğretim Kurumları Yönetmeliği, Mesleki Eğitim Yönetmeliği, 3308 Sayılı Kanun

### 11.1 Koordinatör Öğretmen Görevlendirmesi

- Her işletmeye bir **koordinatör öğretmen** atanır
- Koordinatör öğretmen işletmedeki öğrencilerin devamsızlık, maaş ve fesih süreçlerini takip eder
- Görevlendirme, öğretmenin **ders programındaki boş saatlere** göre yapılır
- İşletmeye **uzaklık** görevlendirmede kritik bir faktördür

### 11.2 Alan Şefliği ve İşletme Dağıtımı

- İşletme dağıtımı **alan şefi** (bölüm başkanı) ve alan öğretmenleri tarafından yapılır
- Dağıtım süreci:
  1. Alan şefi öğretmenlerin ders programlarını ve boş saatlerini inceler
  2. İşletmeleri öğretmenlere dağıtır (mesafe ve ders yükü dengesi gözetilir)
  3. **Zümre karar tutanağı** oluşturulur (dağıtım kararının resmi belgesi)
  4. **Müdür onayı** alınır
  5. Onaylanan dağıtım sisteme işlenir
- Öğrenci fesih/transfer durumunda görevlendirme otomatik güncellenir

### 11.3 Ek Ders Saati Sınırları

- İşletmeye yapılan ziyaretler için verilen ek ders saati mevzuatla belirlenmiştir
- İşletmedeki bir ders saati **60 dakikadır** (MEB Ortaöğretim Kurumları Yönetmeliği Madde 9/3)
- Toplam haftalık ek ders saati **azami sınırı** aşılamaz
- Alan şefliği görev saatleri toplam ders yükünden **düşülür** (şeflik indirimi)

### 11.4 Koordinatörlük Saati Formülü (Mesafeye Göre)

**Kaynak:** MEB mevzuatı — Mesleki Eğitim Yönetmeliği

Her işletmeye verilebilecek **maksimum koordinatörlük saati** okula olan rota mesafesine göre belirlenir:

|Okula Uzaklık|Verilebilecek Maks. Saat|
|---|---|
|≤ 1 km|2 saat|
|≤ 3 km|4 saat|
|≤ 5 km|6 saat|
|> 5 km|8 saat|

**Mesafe hesaplama:** OSRM (Open Source Routing Machine) ile **rota bazlı gerçek yol mesafesi** hesaplanır. Kuş uçuşu (Haversine) değildir. OSRM erişilemezse Haversine fallback olarak kullanılır.

**Lokasyonu olmayan işletmeler:** Manuel mesafe girişi yapılabilir. Manuel girilen mesafe otomatik hesaplama tarafından ezilmez (`IsManualDistance = true`).

### 11.5 Toplam Koordinatörlük Ders Yükü Hesabı

**Hesaplama alan bazlıdır.** Tüm koordinatörlük ders yükü hesabı **alan** (`FieldOfStudy.Code`: EET, BYT, MKT vb.) bazında yapılır. Dallar (ELOHAB, ELKTES, ENDBAK vb.) ayrı hesaplanmaz — aynı alandaki tüm dallar birlikte değerlendirilir.

**Yasal dayanak:** Norm Kadro Yönetmeliği Madde 19 (atölye şefi), Madde 20 (alan şefi), Madde 22 (grup hesaplama).

**Formül (Ders Yükü Havuzu):**

```text
Adım 1: Şeflik Saatleri
  ŞeflikToplamı = (AlanŞefiSayısı × 10) + (AtölyeŞefiSayısı × 6)

Adım 2: Her sınıf seviyesi için (10, 11, 12):
  GrupSayısı = Madde 22 tablosundan otomatik hesaplama (öğrenci sayısına göre)
  AltToplam = HaftalıkDersSaati × GrupSayısı

Adım 3: Toplam Ders Yükü Havuzu
  TotalWorkloadPool = Σ(AltToplam) + ŞeflikToplamı

KESİN KISIT 1: Σ(TakdirEdilenSaat) ≤ TotalWorkloadPool (havuz kısıtı)
KESİN KISIT 2: Σ(TakdirEdilenSaat) ≤ Σ(MesafeBazlıMaksSaat) (mesafe kısıtı)
```

**Örnek hesaplama (BT alanı):**

```text
Şeflik: 1 alan şefi × 10 + 2 atölye şefi × 6 = 22 saat
10. sınıf: 24 öğrenci → 2 grup × 8 ders = 16 saat
11. sınıf: 18 öğrenci → 2 grup × 8 ders = 16 saat
12. sınıf: 32 öğrenci → 3 grup × 8 ders = 24 saat
Toplam havuz: 22 + 16 + 16 + 24 = 78 saat
```

**Çift kısıt:** İşletmelere toplamda 78 saatten fazla takdir edilen saat verilemez VE mesafe bazlı toplam verilebilir saat de aşılamaz. En kısıtlayıcı olan geçerlidir.

**Backend doğrulama:** `UpdateBusinessAssignedHoursHandler` her iki kısıtı kontrol eder. Aşım durumunda `WorkloadPoolExceeded` hatası fırlatılır.

**Frontend:** "Takdir Edilen Saat" tab'ında iki kart: (1) Alan Yapılandırması — şeflik + sınıf bazlı ders yükü → havuz hesaplama, (2) İşletme Saatleri — işletme bazlı takdir edilen saat tablosu.

**Öğretmen bazında:**

- Her işletmeye 0 ile VerilebilirSaat arasında "takdir edilen" saat atanır
- Toplam dağıtılan saat havuzu aşamaz
- Öğretmenin ders programında boş saati olmayan günlere ziyaret atanamaz

### 11.6 Örgün ve MESEM Programı Farkı

**Örgün program:** Normal mesleki lise — grup oluşturma kurallarına göre ders yükü hesaplanır.

**MESEM programı (Mesleki Eğitim Merkezi):** Çırak/kalfa eğitimi — grup kuralları farklıdır.

**Kritik kural:** Bir alanda hem örgün hem MESEM programı varsa, MESEM koordinatörlük saati örgünün azami ek ders hesabına **eklenir**.

### 11.7 Grup Oluşturma Kuralları (Norm Kadro Yönetmeliği Madde 22/1-ç)

#### Örgün Program

|Sınıf|Öğrenci Aralığı|Grup Sayısı|
|---|---|---|
|9. sınıf|10-21|1|
|9. sınıf|21-31|2|
|9. sınıf|31+|3|
|10-12. sınıf|8-17|1|
|10-12. sınıf|17-25|2|
|10-12. sınıf|25-33|3|
|10-12. sınıf|33+|4|

- Bir şubede **maksimum 4 grup** (kaynaştırma öğrenci istisnası ile 5)

#### MESEM Programı (Madde 22/2)

- Şubeler gruplara **bölünmez**
- Öğrenci sayısı grup oluşturma sayısının altındaysa işletmelerde meslek eğitimi dersi dışındaki alan/dal dersleri ders yükü hesabına dahil **edilmez**
- İşletmelerde meslek eğitimi ders yükü için çırak sayısına göre grup:

|Çırak Sayısı|Grup|
|---|---|
|10-41|1|
|41-81|2|
|81-121|3|
|121-161|4|
|161-201|5|
|201-241|6|
|241-281|7|
|281-321|8|
|321-361|9|
|361-401|10|
|401-441|11|
|441+|12|

### 11.8 Yetki Modeli

|Rol|Yetki|Açıklama|
|---|---|---|
|Alan Şefi (DepartmentHead)|`department:*`|Tüm dağıtım işlemleri|
|Müdür (InstitutionManager)|`department:*` + `coordinator:*`|Tam yetki|
|Müdür Yardımcısı (InstitutionStaff)|`department:*`|Dağıtımı görme/yönetme|
|Koordinatör Öğretmen (Teacher)|`coordinator:schedule:manage`|Kendi ders programı + ziyaret|

### 11.9 İş Yükü Hesaplama

**Formül (Azami Ek Ders):**

```text
KullanılabilirEkDersSaati = AzamiEkDersSaati - ŞeflikIndirimSaati
  - Meslek lisesi: büyükşehir 20h, diğer 16h
  - MESEM: büyükşehir 24h, diğer 18h
KullanılanSaat = Σ(AtananİşletmeZiyaretSaatleri)
KalanSaat = KullanılabilirEkDersSaati - KullanılanSaat
```

**Şeflik indirimi:** Alan şefi 10 saat, atölye şefi 6 saat toplam ders yükünden düşülür.

**Kısıtlamalar:**

- `KullanılanSaat ≤ KullanılabilirEkDersSaati` (aşım durumunda sistem uyarı verir)
- Alan bazlı toplam: `Σ(TakdirEdilen) ≤ TotalWorkloadPool` (bkz. 11.5)
- Öğretmenin ders programında boş saati olmayan günlere ziyaret atanamaz
- Öğrenci sayısı ve işletme kapasitesi dikkate alınır

### 11.10 Teknik Altyapı

#### OSRM Entegrasyonu (Rota Bazlı Mesafe)

- **OSRM** (Open Source Routing Machine) Podman container olarak çalışır
- **Harita verisi:** Geofabrik Türkiye haritası (OpenStreetMap) — ilk çalıştırmada otomatik indirilir
- **Algoritma:** MLD (Multi-Level Dijkstra) — hızlı rota hesaplama
- **API:** `/route/v1/driving` (tekli), `/table/v1/driving` (batch — tek istekte N mesafe)
- **Fallback:** OSRM erişilemezse Haversine (kuş uçuşu) kullanılır
- **Backend servisi:** `IOsrmDistanceService` / `OsrmDistanceService` (HttpClient, DI registered)
- **Config:** `appsettings.json` → `Osrm:BaseUrl`

#### Koordinasyon Modülü Veri Modeli

- `CoordinationConfig` — Kurum başına tek document (mesafe-saat kuralları, azami haftalık saat)
- `BusinessCoordinationView` — İşletme-öğretmen atama read model (denormalize, mesafe, saat, alan bilgisi)
- `TeacherSchedule` — Öğretmen haftalık ders programı (5 gün × N ders saati, Occupied/Free)

#### Öğretmen Ders Programı Oluşturma Süreci

Öğretmen ders programı, koordinatörlük işletme dağıtımının **ön koşuludur**. Öğretmenin boş saatleri bilinmeden işletme ataması yapılamaz.

**Ön koşul — Kurum Ders Saati Ayarı:**

- Kurum yöneticisi `ScheduleConfiguration`'da günlük ders sayısını belirler (ör: 8, 9 veya 10)
- Bu ayar yapılmadan ders programı ekranı kullanılamaz (uyarı gösterilir)
- Her kurum kendi günlük ders saatini ayarlar — okullar arası farklılık olabilir

**Ders Programı Girişi:**

1. Alan şefi veya koordinatör öğretmen "Ders Programı" ekranından öğretmen seçer
2. Dönem (Güz/Bahar) ve akademik yıl seçilir
3. 5×N ızgara açılır (5 gün × N ders saati — N kurumun `dailyPeriodCount` ayarı)
4. Her hücre için durum belirlenir:
   - **Dolu (Occupied):** Öğretmenin dersi var → ders adı girilir (ör: "Matematik", "Fizik")
   - **Boş (Free):** Öğretmenin dersi yok → işletme atanabilir
5. Kaydet → `UpsertTeacherSchedule` komutu ile `TeacherSchedule` document'ı oluşturulur/güncellenir

**Boş Saat Kuralları:**

- Boş saatler otomatik olarak belirlenir — dolu olmayan her saat "boş" sayılır
- Günlük 9 saate göre program girilmişse, 8 ders saatlik bir okula göre işletme verilemez — bu kontrolün temeli ders programıdır
- Boş saat = işletme ziyareti için kullanılabilir saat
- Bir öğretmenin boş saati yoksa o güne işletme atanamaz

**Veri Modeli (`TeacherSchedule`):**

- `Id` — Document ID
- `TeacherId` — Öğretmen
- `InstitutionId` — Kurum
- `AcademicPeriodId` — Dönem
- `AcademicYear` + `Semester` — Yıl ve dönem
- `WeeklySchedule` — 5 günlük program dizisi
  - Her gün: `Day` (Monday-Friday) + `Periods` dizisi
  - Her period: `PeriodNumber` (1..N), `Status` (Occupied/Free), `CourseName` (nullable), `AssignedBusinessId` (nullable)

**API Endpoint'leri:**

- `POST /api/coordination/teachers/{teacherId}/schedule` — Program oluştur/güncelle
- `GET /api/coordination/teachers/{teacherId}/schedule?year=2025&semester=Fall` — Program getir
- `GET /api/coordination/teachers/{teacherId}/free-slots?year=2025&semester=Fall` — Boş saatleri listele

**Yetki:** `coordinator:schedule:manage` — Alan şefi, müdür, müdür yardımcısı ve koordinatör öğretmen erişebilir

**İşletme Dağıtımı ile İlişkisi:**

- Ders programı girildikten sonra boş saatler `GetTeacherFreeSlots` ile sorgulanır
- İşletme dağıtımı ekranında öğretmen seçildiğinde, o öğretmenin boş günleri kontrol edilir
- Boş saati olmayan güne işletme atanamaz (backend validation)
- İşletme atandığında ilgili period'un `AssignedBusinessId`'si güncellenir

### 11.5 Koordinatör Öğretmen Formları

Koordinatör öğretmen aşağıdaki formları üretir:

| Form | Açıklama | Periyot |
| ---- | -------- | ------- |
| Günlük Form | İşletme ziyareti sırasında doldurulan günlük yoklama ve gözlem formu | Her ziyarette |
| Aylık Form | Aylık öğrenci devam durumu, maaş bilgisi ve sözleşme durumu özet formu | Ayda 1 |
| Devamsızlık Çizelgesi | Ay boyunca günlük bazda öğrenci devam/devamsızlık tablosu | Ayda 1 |

- Tüm formlar **QuestPDF** ile PDF olarak üretilir (Reporting modülü)
- Formlar ilgili denormalize read model'lardan oluşturulur
- Günlük formda: öğrenci yoklaması, devamsızlık türü, gözlem notları
- Aylık formda: toplam ziyaret günü, öğrenci bazlı devam/devamsızlık sayıları, maaş tutarları, sözleşme durumları
- Devamsızlık çizelgesinde: her gün için durum kodu (V=var, Y=yok, M=mazeretli, S=sağlık raporu, T=tatil)

### 11.6 Dağıtım Onay Süreci

```text
TASLAK → ZÜMRE_KARARI_ALINDI → MÜDÜR_ONAY_BEKLİYOR → ONAYLANDI / REDDEDİLDİ
```

- Alan şefi dağıtım taslağını oluşturur
- Zümre toplantısında karar alınır ve tutanak notu eklenir
- Müdüre onay gönderilir
- Müdür onaylarsa dağıtım aktifleşir ve öğretmen-işletme atamaları güncellenir
- Müdür reddederse alan şefine geri döner, revize edilir
