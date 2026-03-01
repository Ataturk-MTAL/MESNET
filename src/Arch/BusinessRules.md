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
3. Tarih kısıtlı bir gün mü? (`WorkCalendar`)
4. Devamsızlık türü geçerli mi? (`AbsenceType` SmartEnum)

---

## 6. Maaş Hesaplama Kuralları

**Yasal Dayanak:** 3308 Sayılı Kanun Madde 25

### 6.1 Taban Ücret Hesabı

| İşletme Tipi | Oran | Formül |
| ------------ | ---- | ------ |
| 20+ personel çalıştıran | %30 | Net Asgari Ücret × 0.30 |
| 20'den az personel çalıştıran | %15 | Net Asgari Ücret × 0.15 |
| MEM 12. sınıf (kalfalık yeterliği) | %50 | Net Asgari Ücret × 0.50 |

> **Not:** Bu oranlar yasal asgari değerlerdir. İşletmeler daha yüksek ücret ödeyebilir.

### 6.2 Devamsızlık Kesintisi

Özürsüz devamsızlık ve ücretsiz izin günlerinde ücret kesilir.

**Formül:**

```
GünlükÜcret = AylıkTabanÜcret / 30
KesintiBedeli = GünlükÜcret × MazeretsizDevamsızGünSayısı
ÖdenecekÜcret = AylıkTabanÜcret - KesintiBedeli
```

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
DevletKatkısı = ÖdenecekÜcret × DevletKatkısıOranı
İşverenPayı = ÖdenecekÜcret - DevletKatkısı
```

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
- Dekont onay zinciri: İşletme → Öğretmen → Kurum → Öğrenci onayı

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

### 11.4 İş Yükü Hesaplama

**Formül:**

```text
KullanılabilirEkDersSaati = AzamiEkDersSaati - ŞeflikIndirimSaati
KullanılanSaat = Σ(AtananİşletmeZiyaretSaatleri)
KalanSaat = KullanılabilirEkDersSaati - KullanılanSaat
```

**Kısıtlamalar:**

- `KullanılanSaat ≤ KullanılabilirEkDersSaati` (aşım durumunda sistem uyarı verir)
- İşletmeye uzaklık `AzamiMesafe` değerini aşamaz
- Öğretmenin ders programında boş saati olmayan günlere ziyaret atanamaz
- Öğrenci sayısı ve işletme kapasitesi dikkate alınır

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
