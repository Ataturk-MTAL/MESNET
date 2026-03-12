---
title: Senaryolar
---

# MESNET PROJESİ UYGULAMA SENARYOLARI

> **Kapsam Notu:** Bu doküman Phase 1 (çekirdek staj süreçleri) ve Phase 2 (blockchain/NFT) olarak ayrılmıştır.
> Phase 1'in ana odağı: sözleşme yönetimi, devamsızlık takibi, dekont/maaş süreçleri, staj fesih işlemleri, yeni işletmeye yerleşme ve lokasyon bazlı işletme yönetimidir.
> Blockchain ve NFT sertifika süreçleri Phase 2 kapsamındadır ve Phase 1 tamamlandıktan sonra ele alınacaktır.

## İşletme Modülü Senaryoları

### 1. Maaş Ödeme Dekontu Yükleme

**Senaryo:** İşletme, maaş ödemesi için bir dekont yükleyebilir.

- **Aktör:** İşletme Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Ödemeler' bölümüne gidin.
  2. 'Dekont Yükle'ye tıklayın.
  3. Yerel sistemden dekont dosyasını seçin.
  4. 'Gönder' butonuna tıklayın.
- **Sonuç:** Dekont yüklenir ve sistemde saklanır.

### 2. Kurumdan Öğrenci Talep Etme

**Senaryo:** İşletme, kurumdan öğrenci talep edebilir.

- **Aktör:** İşletme Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Öğrenci Talepleri' bölümüne gidin.
  2. 'Öğrenci Talep Et' butonuna tıklayın.
  3. Öğrenci talebi için gerekli bilgileri doldurun.
  4. 'Gönder' butonuna tıklayın.
- **Sonuç:** Öğrenci talebi kuruma gönderilir.

### 3. Öğrencileri Listeleme

**Senaryo:** İşletme, bünyesindeki tüm öğrencileri listeleyebilir.

- **Aktör:** İşletme Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Öğrenciler' bölümüne gidin.
  2. 'Öğrencileri Listele' butonuna tıklayın.
- **Sonuç:** Tüm öğrencilerin listesi görüntülenir.

### 4. Devamsızlık Kaydı Oluşturma

**Senaryo:** İşletme, bir öğrenci için devamsızlık kaydı oluşturabilir.

- **Aktör:** İşletme Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Devamsızlık' bölümüne gidin.
  2. 'Devamsızlık Kaydı Oluştur' butonuna tıklayın.
  3. Kendi bünyesindeki öğrenciyi seçin (sadece işletmeye yerleştirilmiş öğrenciler listelenir).
  4. İşletme bilgisi otomatik doldurulur.
  5. Tarih, devamsızlık türü ve detayları doldurun.
  6. 'Gönder' butonuna tıklayın.
- **Sonuç:** Devamsızlık kaydı `Onay Bekliyor` (Pending) durumunda oluşturulur. Koordinatör öğretmene SSE bildirim gönderilir. Koordinatör onayladığında kayıt `Kaydedildi` (Recorded) durumuna geçer.

### 5. İşletme Bilgilerini Güncelleme

**Senaryo:** İşletme, bilgilerini güncelleyebilir.

- **Aktör:** İşletme Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'İşletme Bilgileri' bölümüne gidin.
  2. 'Bilgileri Düzenle' butonuna tıklayın.
  3. Gerekli bilgileri güncelleyin.
  4. 'Kaydet' butonuna tıklayın.
- **Sonuç:** İşletme bilgileri sistemde güncellenir.

### 6. İşletme Belge Yönetimi

**Senaryo:** İşletme, usta öğretici belgelerini yükleyebilir, güncelleyebilir ve durumunu takip edebilir.

- **Aktör:** İşletme Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'İşletme Belgeleri' bölümüne gidin
  2. Belge işlemleri:
     - Yeni Belge Yükleme:
       - Belge türünü seçin:
         - Ustalık Belgesi
         - Usta Öğreticilik Belgesi
         - Diğer Belgeler
       - Taranmış belgeyi yükleyin
       - Belge geçerlilik tarihlerini girin
       - Varsa ek açıklamaları yazın
       - 'Yükle' butonuna tıklayın
     - Belge Güncelleme:
       - Mevcut belgeyi seçin
       - 'Güncelle' butonuna tıklayın
       - Yeni belgeyi yükleyin
       - Geçerlilik tarihlerini güncelleyin
       - Güncelleme nedenini belirtin
  3. Belge durumunu takip edin:
     - Onay Bekliyor
     - Onaylandı
     - Reddedildi (Gerekçeli)
     - Düzeltme İstendi
  4. Gerektiğinde düzeltme/güncelleme yapın
- **Sonuç:** Belgeler sisteme yüklenir ve kurum onayına sunulur.

## Stajyer Modülü Senaryoları

### 1. Aylık Maaş Görüntüleme

**Senaryo:** Stajyer, aylık hak ettiği maaşı görüntüleyebilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Maaşlar' bölümüne gidin.
  2. Aylık maaş bilgilerini görüntüleyin.
- **Sonuç:** Stajyer, aylık hak ettiği maaşı görüntüler.

### 2. İş Yeri Devamsızlıklarını Görüntüleme

**Senaryo:** Stajyer, iş yeri devamsızlıklarını görüntüleyebilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Devamsızlıklar' bölümüne gidin.
  2. İş yeri devamsızlık bilgilerini görüntüleyin.
- **Sonuç:** Stajyer, iş yeri devamsızlıklarını görüntüler.

### 3. Sağlık Raporu Yükleme

**Senaryo:** Stajyer, aldığı sağlık raporunu yükleyebilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Sağlık Raporları' bölümüne gidin.
  2. 'Rapor Yükle' butonuna tıklayın.
  3. Yerel sistemden sağlık raporu dosyasını seçin.
  4. 'Gönder' butonuna tıklayın.
- **Sonuç:** Sağlık raporu yüklenir ve sistemde saklanır.

### 4. Kurum Bilgilerini Görüntüleme

**Senaryo:** Stajyer, kurum bilgilerini görüntüleyebilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Kurum Bilgileri' bölümüne gidin.
  2. Kurum bilgilerini görüntüleyin.
- **Sonuç:** Stajyer, kurum bilgilerini görüntüler.

### 5. Maaş Onayı Verme

**Senaryo:** Stajyer, maaş aldığına dair onay verebilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Maaş Onayı' bölümüne gidin.
  2. Maaş bilgilerini kontrol edin.
  3. 'Onay Ver' butonuna tıklayın.
- **Sonuç:** Stajyer, maaş aldığına dair onay verir ve sistemde saklanır.

### 6. Staj Başvuru İşlemleri

**Senaryo:** Stajyer, staj başvurusunu sistem üzerinden yapabilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Staj Başvuru' bölümüne gidin
  2. Başvuru işlemleri:
     - Yeni Başvuru:
       - 'Başvuru Oluştur' butonuna tıklayın
       - İşletme tercihlerini belirtin
       - Staj dönemi seçin
       - Alan/Dal bilgilerini kontrol edin
       - Başvuru formunu doldurun
       - 'Gönder' butonuna tıklayın
     - Başvuru Takibi:
       - Başvuru durumunu görüntüleyin
       - İşlem geçmişini inceleyin
       - Varsa eksik belgeleri tamamlayın
     - İşletme Tercihleri:
       - İşletme havuzundan seçim yapın
       - Tercih sıralaması oluşturun
       - Gerekçe belirtin
- **Sonuç:** Staj başvurusu oluşturulur ve takip edilebilir.

### 7. Sözleşme Takibi

**Senaryo:** Stajyer, sözleşme sürecini görüntüleyebilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Sözleşme İşlemleri' bölümüne gidin
  2. Sözleşme bilgilerini görüntüleyin:
     - Sözleşme durumu
     - İmza süreci aşamaları
     - Gerekli belgeler listesi
  3. Belge/imza tamamlama tarihleri
  4. Varsa eksik işlemleri görün
- **Sonuç:** Sözleşme süreci takip edilir.

### 8. Koordinatör İletişimi

**Senaryo:** Stajyer, koordinatör öğretmeni ile iletişim kurabilir.

- **Aktör:** Stajyer Öğrenci
- **Ön Koşul:** Stajyer sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'İletişim' bölümüne gidin
  2. İletişim seçenekleri:
     - Mesaj Gönderme:
       - Koordinatör öğretmeni seçin
       - Mesaj içeriğini yazın
       - 'Gönder' butonuna tıklayın
     - Sorun Bildirimi:
       - Sorun türünü seçin
       - Detaylı açıklama girin
       - Varsa kanıt/ekran görüntüsü ekleyin
       - 'Bildir' butonuna tıklayın
  3. İletişim geçmişini görüntüleyin
- **Sonuç:** Koordinatör öğretmen ile iletişim sağlanır.

## Kurum Modülü Senaryoları
>
> Not: Tüm işlemler için sistem üzerinden verilmiş yetki kontrolleri uygulanır. İşlemler sadece müdür ve yetkilendirilmiş müdür yardımcıları tarafından gerçekleştirilebilir.

### 1. Öğrenci Kayıt İşlemleri

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel öğrencileri sisteme kaydedebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Öğrenci kayıt işlemleri için yetkilendirilmiş olmalıdır.
- **Adımlar:**
  1. 'Öğrenci İşlemleri' bölümüne gidin.
  2. 'Yeni Kayıt' veya 'Toplu Kayıt' seçeneğini seçin.
  3. Alan/Dal ve öğrenim türü bilgilerini girin.
  4. Öğrenci bilgilerini doldurun.
  5. 'Kaydet' butonuna tıklayın.
- **Sonuç:** Öğrenci(ler) sisteme kaydedilir.

### 2. Kurum Bilgileri Yönetimi

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel kurum bilgilerini görüntüleyip güncelleyebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Kurum bilgilerini yönetme yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Kurum Bilgileri' bölümüne gidin.
  2. Mevcut bilgileri görüntüleyin.
  3. 'Düzenle' butonuna tıklayın.
  4. Bilgileri güncelleyin.
  5. 'Kaydet' butonuna tıklayın.
- **Sonuç:** Kurum bilgileri güncellenir.

### 3. Devamsızlık ve Maaş Yönetimi

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel devamsızlıkları düzenleyip maaş hesaplaması yapabilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Devamsızlık ve maaş yönetimi yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Devamsızlık/Maaş İşlemleri' bölümüne gidin.
  2. Öğrenci devamsızlıklarını düzenleyin.
  3. 'Maaş Hesapla' butonuna tıklayın.
  4. Hesaplanan maaşları onaylayın.
  5. 'İlgili Modüllere Aktar' butonuna tıklayın.
- **Sonuç:** Maaşlar hesaplanır ve ilgili modüllere aktarılır.

### 4. Asgari Ücret Güncelleme

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel asgari ücret bilgisini güncelleyebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Asgari ücret güncelleme yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Sistem Ayarları' bölümüne gidin.
  2. 'Asgari Ücret Güncelle' seçeneğini seçin.
  3. Yeni asgari ücret tutarını girin.
  4. 'Kaydet' butonuna tıklayın.
- **Sonuç:** Asgari ücret bilgisi güncellenir.

### 5. İşletme İletişimi

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel işletmelere mesaj gönderebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - İşletme iletişimi yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'İşletme İletişim' bölümüne gidin.
  2. İşletme(leri) seçin.
  3. Mesaj içeriğini yazın.
  4. 'Gönder' butonuna tıklayın.
- **Sonuç:** Mesaj işletmelere iletilir.

### 6. Öğretmen Kayıt İşlemleri

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel öğretmenleri sisteme kaydedebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Öğretmen kayıt işlemleri için yetkilendirilmiş olmalıdır.
- **Adımlar:**
  1. 'Öğretmen İşlemleri' bölümüne gidin.
  2. 'Yeni Kayıt' veya 'Toplu Kayıt' seçeneğini seçin.
  3. Alan bilgilerini girin.
  4. Öğretmen bilgilerini doldurun.
  5. 'Kaydet' butonuna tıklayın.
- **Sonuç:** Öğretmen(ler) sisteme kaydedilir.

### 7. Öğretmen Raporları Oluşturma

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel öğretmenler için raporlar oluşturabilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Öğretmen raporları oluşturma yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Öğretmen Raporları' bölümüne gidin.
  2. Rapor türünü seçin (Aylık/Günlük/Devamsızlık).
  3. Bireysel veya toplu rapor seçeneğini belirleyin.
  4. İlgili öğretmen(leri) seçin.
  5. 'Rapor Oluştur' butonuna tıklayın.
  6. Karekodlu raporu görüntüleyin ve yazdırın.
- **Sonuç:** Karekodlu rapor oluşturulur ve yazdırılabilir.

### 8. Evrak Teslim Takibi

**Senaryo:** Kurum yönetimince yetkilendirilmiş personel (müdür yardımcıları vb.) işletme onaylı evrakların teslimini mobil uygulama üzerinden karekod okutarak veya web/mobil uygulama üzerinden manuel olarak yapabilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Personele evrak teslim alma yetkisi verilmiş olmalıdır.
  - Karekod okutma için mobil uygulama yüklenmiş olmalıdır.
- **Adımlar:**
  1. Teslim yöntemini seçin:
     - Karekod ile Teslim (Mobil):
       - Mobil uygulamayı açın.
       - 'Evrak Takip' bölümüne gidin.
       - 'Karekod Okut' butonuna tıklayın.
       - Evrak üzerindeki karekodu okutun.
       - Evrak bilgilerini onaylayın.
     - Manuel Teslim (Web/Mobil):
       - 'Evrak Takip' bölümüne gidin.
       - 'Manuel Teslim' butonuna tıklayın.
       - İlgili öğretmeni seçin.
       - Teslim alınan evrakları işaretleyin.
       - Teslim tarihi otomatik olarak kaydedilir.
       - 'Teslim Alındı' butonuna tıklayın.
- **Sonuç:** Evraklar, teslim alan yetkili personelin bilgisiyle birlikte sistemde "Teslim Alındı" olarak işaretlenir.

### 9. İşletme Ziyaret Programı Yönetimi

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel öğretmenlerin öğrenci ziyaret programını ders programı formatında oluşturabilir ve yönetebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Program yönetimi yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'İşletme Ziyaret Programı' bölümüne gidin.
  2. Program oluşturma adımları:
     - Öğretmen Seçimi:
       - İlgili öğretmeni listeden seçin
       - Öğretmenin mevcut programını görüntüleyin
     - Program Tablosu:
       - Haftalık program tablosunu görüntüleyin (Pazartesi-Cuma / 08:00-17:00)
       - Sağ panelde iki sekme bulunur:
         - Öğrenciler Sekmesi:
           - Atanmamış öğrencilerin listesi
           - Her öğrenci kartında:
             - Öğrenci Adı Soyadı
             - Sınıfı ve Alanı
             - İşletme Bilgileri
             - Usta Öğretici Bilgisi
         - İşletmeler Sekmesi:
           - Ziyaret atanmamış işletmelerin listesi
           - Her işletme kartında:
             - İşletme Adı
             - Adres Bilgileri
             - Telefon
             - İşletmedeki Toplam Öğrenci Sayısı
     - Atama İşlemleri:
       - Tekil Öğrenci Ataması:
         - Öğrenciler sekmesinden öğrenciyi seçin
         - Tablodaki uygun zaman dilimine sürükleyip bırakın
       - İşletme Bazlı Toplu Atama:
         - İşletmeler sekmesinden işletmeyi seçin
         - Tablodaki uygun zaman dilimine sürükleyip bırakın
         - Sistem otomatik olarak o işletmedeki tüm öğrencileri seçilen zaman dilimine atar
       - Atama Sonrası:
         - Atanan öğrenci/işletme listeden otomatik kaldırılır
         - Tabloda işletme ve öğrenci bilgileri görüntülenir
     - Program Düzenleme:
       - Atanmış öğrenci veya işletmeyi başka bir zaman dilimine sürükleyebilirsiniz
       - İşletme taşındığında tüm öğrencileri birlikte taşınır
       - Atamayı iptal etmek için tablodaki öğeyi çıkarın
     - Çakışma Kontrolü:
       - Öğretmen programı çakışmaları kontrolü
       - Öğrenci müsaitlik kontrolü
       - İşletme bazlı atamalarda grup kontrolü
       - Çakışma durumunda detaylı uyarı gösterimi
  3. 'Kaydet' butonuna tıklayın
  4. Program onayı verin
- **Sonuç:** İşletme ziyaret programı ders programı formatında, öğrenci bazlı olarak oluşturulur ve raporlarda kullanılmak üzere sistemde saklanır.

### 10. Öğretmen Detay Bilgileri Görüntüleme

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel öğretmenlerin detaylı bilgilerini görüntüleyebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Öğretmen bilgilerini görüntüleme yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Öğretmen İşlemleri' bölümüne gidin.
  2. 'Öğretmen Detayları' seçeneğine tıklayın.
  3. İlgili öğretmeni seçin.
  4. Detaylı bilgileri görüntüleyin:
     - Sorumlu olduğu işletme sayısı
     - Koordinatörlük ek ders ücreti
     - Toplam öğrenci sayısı
     - Haftalık işletme ziyaret programı
     - Aylık işletme ziyaret çizelgesi
  5. İsterseniz rapor olarak yazdırın.
- **Sonuç:** Öğretmenin detaylı bilgileri görüntülenir ve raporlanabilir.

### 11. Sözleşme İşlemleri

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel öğrenci-işletme sözleşme sürecini yönetebilir ve işletme belgelerini yükleyebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Sözleşme işlemleri yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Sözleşme İşlemleri' bölümüne gidin
  2. Öğrenci seçimi yapın
  3. İşletme bilgilerini ve belgelerini kontrol edin:
     - İşletme tarafından yüklenmiş belgeleri inceleyin
     - Belgelerin geçerliliğini kontrol edin
     - Eksik belgeler için:
       - 'Belge Yükle' butonuna tıklayın
       - Belge türünü seçin (Ustalık/Usta Öğreticilik/Mezuniyet)
       - Taranmış belgeyi yükleyin
       - Geçerlilik tarihlerini girin
       - 'Kaydet' butonuna tıklayın
     - Gerekirse belge düzeltmesi talep edin
  4. Sözleşme sürecini başlatın:
     - İşyeri sözleşmesini oluşturun
     - Gerekli imzaları tamamlayın
     - Sözleşme tarihlerini belirleyin
  5. Tüm belgeleri onaylayın
- **Sonuç:** Sözleşme süreci tamamlanır ve belgeler onaylanır.

### 12. İşletme Belge Doğrulama

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel işletme tarafından yüklenen belgeleri doğrulayabilir ve onaylayabilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Belge doğrulama yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'İşletme Belgeleri' bölümüne gidin
  2. Onay bekleyen belgeleri listeleyin:
     - İşletme tarafından yeni yüklenen belgeler
     - Güncellenen belgeler
     - Düzeltme sonrası tekrar yüklenen belgeler
  3. Belge kontrolü yapın:
     - Belge içeriğini inceleyin
     - Geçerlilik tarihlerini kontrol edin
     - Belge formatını kontrol edin
  4. İşlem seçin:
     - Onayla:
       - Belgeyi kontrol edin
       - Onay notunu girin (isteğe bağlı)
       - 'Onayla' butonuna tıklayın
     - Reddet:
       - Red gerekçesini yazın
       - 'Reddet' butonuna tıklayın
     - Düzeltme İste:
       - Düzeltme talebini detaylandırın
       - 'Düzeltme İste' butonuna tıklayın
  5. İşlem sonrası durum güncellemesi yapın
- **Sonuç:** İşletme belgeleri kontrol edilir ve uygun işlem yapılır.

### 13. Dekont Takip ve Onay

**Senaryo:** Kurum yönetiminin yetkilendirdiği personel dekontları listeleyebilir ve onaylayabilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Dekont işlemleri yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Dekont İşlemleri' bölümüne gidin
  2. Dekont listeleme:
     - Öğretmen onaylı dekontlar
     - Bekleyen dekontlar
     - Reddedilen dekontlar
  3. Dekont inceleme:
     - Dekont detaylarını görüntüle
     - Öğrenci bilgilerini kontrol et
     - Ödeme tutarını doğrula
  4. İşlem yapma:
     - Onayla:
       - Dekont kontrolünü tamamla
       - İşletmeyi ödeme listesine ekle
       - 'Onayla' butonuna tıkla
     - Reddet:
       - Red gerekçesini gir
       - 'Reddet' butonuna tıkla
  5. Ödeme listesi oluştur
- **Sonuç:** Dekontlar onaylanır ve ödeme listesi hazırlanır.

### 14. Haftalık Ziyaret Ataması

**Senaryo:** Müdür yardımcısı, koordinatör öğretmenlerin haftalık işletme ziyaret atamalarını oluşturabilir.

- **Aktör:** Müdür / Müdür Yardımcısı
- **Ön Koşul:**
  - İşletme-öğretmen atamaları yapılmış olmalıdır (İşletme Dağıtımı tamamlanmış).
  - Öğretmen ders programları girilmiş olmalıdır.
- **Adımlar:**
  1. 'Koordinasyon > Haftalık Ziyaretler' bölümüne gidin.
  2. Hafta seçimi yapın (yıl + hafta numarası veya tarih seçici).
  3. Kapsam belirleyin:
     - Tüm meslek öğretmenleri (tek tuşla)
     - Alan bazında (alan seçerek)
     - Tek öğretmen (öğretmen seçerek)
  4. 'Ziyaret Oluştur' butonuna tıklayın.
  5. Sistem, seçili kapsamdaki her öğretmen-işletme-gün-saat kombinasyonu için ayrı bir ziyaret kaydı (günlük form) oluşturur.
  6. Oluşturulan ziyaretleri tabloda görüntüleyin (öğretmen, işletme, tarih, ders saati).
  7. Hatalı oluşturulmuşsa planı silin ve yeniden oluşturun.
- **Sonuç:** Her ziyaret kaydı benzersiz ID'ye sahiptir. Bu ID, günlük form raporlarında QR kod olarak basılır. Öğretmenler imzalı formları işletmeye götürür.

### 15. Alan/Dal Aktifleştirme ve Yönetimi

**Senaryo:** Kurum yönetimi MEB resmi alan/dal kataloğundan kurumun açtığı alanları ve dalları aktifleştirebilir, pasifleştirebilir ve yönetebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Personel sisteme giriş yapmış olmalıdır.
  - Alan/dal yönetimi yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Alan/Dal Yönetimi' bölümüne gidin.
  2. Eğitim türünü seçin (MTAL - Örgün / MESEM).
  3. MEB resmi alan kataloğu listelenir.
  4. Alan aktifleştirme:
     - Katalogdan alanı seçin.
     - 'Aktifleştir' butonuna tıklayın.
     - Alanın dalları listelenir.
     - Aktif olacak dalları seçin.
     - 'Kaydet' butonuna tıklayın.
  5. Alan pasifleştirme:
     - Kurumun aktif alanları listesinden alanı seçin.
     - 'Pasifleştir' butonuna tıklayın.
     - Onay verin.
  6. Dal güncelleme:
     - Aktif alanı seçin.
     - 'Dalları Düzenle' butonuna tıklayın.
     - Aktif/pasif dalları güncelleyin.
     - 'Kaydet' butonuna tıklayın.
- **Sonuç:** Kurumun alan/dal yapısı güncellenir. İlgili event'ler (BranchActivated, BranchDeactivated, BranchSpecializationsUpdated) yayınlanır.

## Öğretmen Modülü Senaryoları

### 1. Öğrenci ve İşletme Listeleme

**Senaryo:** Öğretmen, sorumlu olduğu öğrenci ve işletmeleri listeleyebilir.

- **Aktör:** Öğretmen
- **Ön Koşul:** Öğretmen sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Öğrenci ve İşletme' bölümüne gidin
  2. Listeleme seçenekleri:
     - Öğrenci Bazlı Liste:
       - Öğrenci bilgileri
       - Öğrenci işletme bilgisi
       - Staj durumu
     - İşletme Bazlı Liste:
       - İşletme bilgileri
       - İşletmedeki öğrenciler
       - Usta öğretici bilgileri
  3. İsterseniz liste çıktısı alın
- **Sonuç:** Öğrenci ve işletme bilgileri görüntülenir.

### 2. Rapor İşlemleri

**Senaryo:** Öğretmen, koordinatörlük raporlarını çıktı alıp işletmeye fiziksel olarak teslim edebilir.

- **Aktör:** Öğretmen
- **Ön Koşul:** Öğretmen sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Raporlar' bölümüne gidin
  2. Rapor türünü seçin:
     - Aylık ziyaret raporu
     - Günlük ziyaret raporu
     - Devamsızlık raporu
  3. Rapor yazdırma:
     - Tarih aralığı seçin
     - İşletme seçin
     - İlgili öğrencileri seçin
     - 'Yazdır' butonuna tıklayın
     - Karekodlu raporu yazdırın
  4. Fiziksel onay süreci:
     - İşletmeye raporu götürün
     - İşletme yetkilisine imzalatın
     - Islak imzalı raporu muhafaza edin
  5. Kurum idaresine teslim edin
- **Sonuç:** Karekodlu rapor fiziksel olarak işletmeye onaylatılır.

### 3. Öğrenci Devamsızlık Yönetimi

**Senaryo:** Öğretmen, öğrenci devamsızlıklarını kaydedebilir.

- **Aktör:** Öğretmen
- **Ön Koşul:** Öğretmen sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Devamsızlık' bölümüne gidin
  2. Devamsızlık girişi:
     - Koordine ettiği öğrenciyi seçin (sadece kendi sorumluluğundaki öğrenciler listelenir)
     - İşletme bilgisi otomatik doldurulur
     - Tarih seçin
     - Devamsızlık türünü belirtin
     - Açıklama ekleyin
     - 'Kaydet' butonuna tıklayın
  3. İşletme tarafından girilen devamsızlıkları onaylama:
     - `Onay Bekliyor` durumundaki kayıtları inceleyin
     - Onayla butonuna tıklayın → kayıt `Kaydedildi` durumuna geçer
  4. Devamsızlık raporlama:
     - Kaydedilen devamsızlıkları listeleyin
     - Gerekirse düzenleme yapın
     - Rapor oluşturun
- **Sonuç:** Öğrenci devamsızlığı doğrudan `Kaydedildi` (Recorded) durumunda oluşturulur (onay gerekmez).

### 4. Maaş ve Dekont İşlemleri

**Senaryo:** Öğretmen, öğrenci maaş ve dekont bilgilerini yönetebilir.

- **Aktör:** Öğretmen
- **Ön Koşul:** Öğretmen sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Maaş/Dekont' bölümüne gidin
  2. İşlem seçenekleri:
     - Maaş Bilgileri:
       - Öğrenci maaş listesi
       - Aylık maaş durumu
     - Dekont İşlemleri:
       - İşletme dekontlarını görüntüle
       - Dekont kontrolü yap
       - Dekont onayı ver/reddet
       - Kurum modülüne aktar
  3. Onaylanan dekontları kuruma ilet
- **Sonuç:** Maaş ve dekont işlemleri yönetilir.

## ~~Tenant (Üst Yönetim) Modülü Senaryoları~~ — ⚠️ PHASE 2

> **Bu bölümdeki senaryolar Phase 2'ye ertelenmiştir.** Phase 1'de tek kurum senaryosu ile çalışılacak. Çoklu kurum desteği gerektiğinde bu senaryolar aktifleştirilecektir.

### ~~1. Kurum Yönetimi~~

**Senaryo:** ~~Üst yönetim yetkilisi yeni kurum ekleyebilir, mevcut kurumları yönetebilir.~~

- **Aktör:** Tenant Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış ve tenant yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Kurum Yönetimi' bölümüne gidin
  2. Kurum işlemleri:
     - Yeni Kurum Ekleme:
       - 'Yeni Kurum' butonuna tıklayın
       - Kurum bilgilerini girin:
         - Kurum adı
         - Kurum kodu
         - İletişim bilgileri
         - Adres bilgileri
       - 'Kaydet' butonuna tıklayın
     - Mevcut Kurum Yönetimi:
       - Kurumları listeleyin
       - Kurum detaylarını görüntüleyin
       - Kurum bilgilerini güncelleyin
       - Gerekirse kurumu pasife alın
  3. Değişiklikleri onaylayın
- **Sonuç:** Kurum sisteme eklenir veya güncellenir.

### ~~2. Kurum Yönetici Kullanıcı İşlemleri~~

**Senaryo:** ~~Üst yönetim yetkilisi kurum müdürü için kullanıcı tanımlayabilir.~~

- **Aktör:** Tenant Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış ve tenant yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Kullanıcı Yönetimi' bölümüne gidin
  2. Yönetici kullanıcı işlemleri:
     - Yeni Yönetici Tanımlama:
       - İlgili kurumu seçin
       - 'Yönetici Ekle' butonuna tıklayın
       - Yönetici bilgilerini girin:
         - Ad Soyad
         - T.C. Kimlik No
         - E-posta
         - Telefon
         - Görev unvanı
       - Kullanıcı rolünü 'Kurum Müdürü' olarak atayın
       - Geçici şifre oluşturun
       - 'Kaydet' butonuna tıklayın
     - Mevcut Yönetici İşlemleri:
       - Yönetici listesini görüntüleyin
       - Yönetici bilgilerini güncelleyin
       - Gerektiğinde yönetici hesabını pasife alın
       - Şifre sıfırlama işlemi yapın
  3. İşlemi onaylayın
- **Sonuç:** Kurum müdürü için kullanıcı hesabı oluşturulur veya yönetilir.

### ~~3. Kurum İstatistikleri ve Raporlama~~

**Senaryo:** ~~Üst yönetim yetkilisi tüm kurumların istatistiki verilerini ve süreç loglarını görüntüleyebilir.~~

- **Aktör:** Tenant Yöneticisi
- **Ön Koşul:** Yönetici sisteme giriş yapmış ve tenant yetkisine sahip olmalıdır.
- **Adımlar:**
  1. 'Kurum İstatistikleri' bölümüne gidin
  2. Görüntüleme seçenekleri:
     - Genel İstatistikler:
       - Toplam kurum sayısı
       - Aktif/Pasif kurum durumu
       - Toplam işletme sayısı
       - Toplam öğrenci sayısı
       - Aktif staj süreçleri
     - Kurum Bazlı İstatistikler:
       - İşletme sayıları
       - Öğrenci dağılımları
       - Koordinatör öğretmen sayıları
       - Aktif sözleşmeler
     - Süreç Logları:
       - Sözleşme işlemleri
       - Dekont onayları
       - Evrak teslim süreçleri
       - Kullanıcı işlemleri
  3. Filtreleme ve Raporlama:
     - Tarih aralığı seçimi
     - Kurum bazlı filtreleme
     - Süreç türü seçimi
     - İstatistik raporu oluşturma
     - Log detaylarını dışa aktarma
  4. Detaylı İnceleme:
     - Seçili kuruma ait detaylar
     - İşlem geçmişi
     - Süreç durumları
- **Sonuç:** Kurumlara ait istatistiki veriler ve süreç logları görüntülenir.

## Staj Sözleşme Yönetimi Senaryoları

### 1. Staj Sözleşmesi Oluşturma

**Senaryo:** Kurum, öğrenci ve işletme arasında staj sözleşmesi oluşturulur.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Öğrenci sisteme kayıtlı olmalıdır.
  - İşletme sisteme kayıtlı ve onaylı olmalıdır.
  - İşletme belgeleri (ustalık belgesi, usta öğreticilik belgesi) onaylanmış olmalıdır.
- **Adımlar:**
  1. 'Sözleşme İşlemleri' bölümüne gidin.
  2. 'Yeni Sözleşme Oluştur' butonuna tıklayın.
  3. Öğrenciyi seçin.
  4. İşletmeyi seçin.
  5. Staj dönemi (başlangıç/bitiş tarihi) belirleyin.
  6. Sözleşme şartlarını doldurun.
  7. İmza sürecini başlatın (kurum, işletme, öğrenci/veli).
  8. 'Kaydet' butonuna tıklayın.
- **Sonuç:** Staj sözleşmesi oluşturulur ve imza sürecine alınır.

### 2. Sözleşme İmza Takibi

**Senaryo:** Oluşturulan sözleşmelerin imza durumu takip edilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:** Sözleşme oluşturulmuş olmalıdır.
- **Adımlar:**
  1. 'Sözleşme Takip' bölümüne gidin.
  2. Sözleşme durumlarını görüntüleyin:
     - Taslak
     - İmza Bekliyor (Kurum / İşletme / Öğrenci-Veli)
     - Aktif
     - Askıya Alınmış
     - Feshedilmiş
     - Tamamlanmış
  3. Eksik imzalar için bildirim gönderin.
  4. Tamamlanan sözleşmeleri onaylayın.
- **Sonuç:** Sözleşme imza süreci takip edilir.

## Staj Fesih ve Yerleşme Senaryoları

### 1. Staj Fesih İşlemi

**Senaryo:** Staj sözleşmesi çeşitli nedenlerle feshedilebilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Aktif bir staj sözleşmesi olmalıdır.
  - Fesih nedeni belgelenmelidir.
- **Adımlar:**
  1. 'Sözleşme İşlemleri' bölümüne gidin.
  2. Aktif sözleşmeyi seçin.
  3. 'Fesih İşlemi Başlat' butonuna tıklayın.
  4. Fesih nedenini seçin:
     - Öğrenci talebi
     - İşletme talebi
     - Disiplin kararı
     - Sağlık nedeni
     - İşletme kapanması/taşınması
     - Devamsızlık limiti aşımı
  5. Fesih gerekçesini detaylı yazın.
  6. Varsa destekleyici belgeleri yükleyin.
  7. Fesih tarihini belirleyin.
  8. 'Fesih Onayla' butonuna tıklayın.
- **Sonuç:** Staj sözleşmesi feshedilir, öğrenci yeni işletmeye yerleşme sürecine alınır.

### 2. Yeni İşletmeye Yerleşme

**Senaryo:** Stajı feshedilen veya yeni başlayan öğrenci uygun bir işletmeye yerleştirilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Öğrencinin aktif stajı olmamalı veya fesih süreci tamamlanmış olmalıdır.
  - Uygun işletme(ler) bulunmalıdır.
- **Adımlar:**
  1. 'Öğrenci Yerleştirme' bölümüne gidin.
  2. Yerleştirilecek öğrenciyi seçin.
  3. Uygun işletmeleri filtreleyin:
     - Öğrencinin alan/dalına uygun işletmeler
     - Kontenjan durumu müsait işletmeler
     - Lokasyon bazlı yakın işletmeler
     - Belgeleri onaylı işletmeler
  4. İşletme seçin.
  5. Yeni sözleşme sürecini başlatın.
  6. 'Yerleştir' butonuna tıklayın.
- **Sonuç:** Öğrenci yeni işletmeye yerleştirilir ve sözleşme süreci başlar.

## Lokasyon Bazlı İşletme Yönetimi Senaryoları

### 1. İşletme Konum Kaydı

**Senaryo:** İşletme sisteme kayıt olurken veya sonrasında konum bilgisi kaydedilir.

- **Aktör:** İşletme Yöneticisi / Kurum Personeli
- **Ön Koşul:** İşletme sisteme kayıtlı olmalıdır.
- **Adımlar:**
  1. 'İşletme Bilgileri' bölümüne gidin.
  2. 'Konum Bilgisi' sekmesine tıklayın.
  3. Harita üzerinde işletme konumunu işaretleyin veya adres girerek otomatik konum belirleyin.
  4. Konum bilgisini doğrulayın.
  5. 'Kaydet' butonuna tıklayın.
- **Sonuç:** İşletmenin konum bilgisi kaydedilir.

### 2. Yakın İşletme Arama

**Senaryo:** Kurum veya öğrenci, lokasyon bazlı işletme arayabilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel / Öğrenci
- **Ön Koşul:** İşletmelerin konum bilgileri kayıtlı olmalıdır.
- **Adımlar:**
  1. 'İşletme Ara' bölümüne gidin.
  2. Arama kriterleri:
     - Merkez nokta belirleyin (okul adresi, öğrenci adresi vb.)
     - Mesafe yarıçapı seçin (örn. 5km, 10km, 20km)
     - Alan/dal filtresi uygulayın
     - Kontenjan durumu filtresi uygulayın
  3. Harita ve liste görünümünde sonuçları görüntüleyin.
  4. İşletme detaylarını inceleyin.
- **Sonuç:** Lokasyona uygun işletmeler listelenir.

### 3. Öğretmen Ziyaret Rotası Planlama

**Senaryo:** Koordinatör öğretmenin işletme ziyaret rotası lokasyon bazlı optimize edilir.

- **Aktör:** Kurum Yönetiminin Yetkilendirdiği Personel
- **Ön Koşul:**
  - Öğretmenin sorumlu olduğu işletmeler belirlenmiş olmalıdır.
  - İşletmelerin konum bilgileri kayıtlı olmalıdır.
- **Adımlar:**
  1. 'Ziyaret Planlama' bölümüne gidin.
  2. Öğretmeni seçin.
  3. Harita üzerinde sorumlu işletmeleri görüntüleyin.
  4. Sistem önerilen rota sıralaması sunar:
     - Mesafe optimizasyonu
     - Gün bazlı gruplama
  5. Gerekirse rotayı manuel düzenleyin.
  6. 'Programı Kaydet' butonuna tıklayın.
- **Sonuç:** Öğretmenin ziyaret rotası konum bazlı optimize edilir.

---

## PHASE 2 — Aşağıdaki senaryolar Phase 1 tamamlandıktan sonra ele alınacaktır

---

## ~~Blockchain Modülü Senaryoları~~ (Phase 2)

> **Phase 2:** Bu modül Phase 1 kapsamı dışındadır. Staj süreçleri tamamlanıp stabil hale geldikten sonra implementasyona alınacaktır.

### 1. Blockchain Yeterlilikleri Yönetimi

**Senaryo:** Öğrenci, kurum ve işletme için blockchain tabanlı yeterlilik yönetimi.

- **Aktör:** Kurum Yöneticisi, Öğrenci, İşletme Yetkilisi
- **Ön Koşul:**
  - Sisteme giriş yapılmış olmalıdır
  - Web3 cüzdan bağlantısı yapılmış olmalıdır
- **Adımlar:**
  1. 'Blockchain Yeterlilikleri' bölümüne gidin
  2. Yeterlilik işlemleri:
     - Yeni Yeterlilik Ekleme:
       - Yeterlilik türü seçimi
       - Gerekli bilgilerin girişi
       - Belge yükleme
       - Web3 cüzdan onayı
     - NFT Dönüşüm İşlemleri:
       - Metadata hazırlama
       - NFT basımı ve transfer
       - Doğrulama bilgileri oluşturma
  3. Yeterlilik takibi:
     - Blockchain kayıtları listeleme
     - NFT detayları görüntüleme
     - Doğrulama bağlantıları paylaşma
  4. İşlem raporları:
     - Detaylı işlem geçmişi
     - Gas maliyetleri
     - Durum bilgileri
- **Sonuç:** Yeterlilikler blockchain üzerinde güvenli şekilde saklanır.

### 2. Akıllı Sözleşme İşlemleri

**Senaryo:** Sistem yöneticileri blockchain akıllı sözleşmelerini yönetir.

- **Aktör:** Sistem Yöneticisi
- **Ön Koşul:** Yönetici yetkisi ve Web3 cüzdan bağlantısı
- **Adımlar:**
  1. 'Sözleşme Yönetimi' bölümüne gidin
  2. Sözleşme işlemleri:
     - Yeni sözleşme dağıtımı
     - Sözleşme güncellemeleri
     - Yetki yönetimi
  3. Ağ yönetimi:
     - Node durumu kontrolü
     - Gas optimizasyonu
     - Performans izleme
  4. Raporlama:
     - İşlem logları
     - Hata kayıtları
     - Maliyet analizleri
- **Sonuç:** Blockchain altyapısı etkin şekilde yönetilir.

### 3. NFT Sertifika Sistemi

**Senaryo:** Blockchain modülü, gelen sertifika taleplerini NFT'ye dönüştürür.

- **Aktör:** Blockchain Sistem Yöneticisi
- **Ön Koşul:**
  - NFT basım yetkisi
  - Web3 cüzdan bağlantısı
  - Sertifika talebi gelmiş olmalı
- **Adımlar:**
  1. 'NFT Sertifika İşlemleri' bölümüne gidin
  2. Gelen talepleri listeleyin:
     - Protokol eğitim sertifikaları
     - Mesleki yeterlilik belgeleri
     - Diğer sertifikalar
  3. Sertifika NFT dönüşümü:
     - Talep detaylarını inceleyin
     - Metadata kontrolü yapın:
       - Sertifika türü
       - Program bilgileri
       - Öğrenci bilgileri
       - Geçerlilik kriterleri
     - NFT şablonu seçin/oluşturun
     - Smart contract hazırlığı
  4. Toplu basım işlemi:
     - Gas optimizasyonu yapın
     - Batch mint işlemini başlatın
     - Transfer listesi oluşturun
  5. Öğrenci cüzdanlarına transfer:
     - Toplu transfer işlemi
     - Transfer onayları
     - Hata yönetimi
  6. Doğrulama sistemi:
     - QR kod üretimi
     - Doğrulama sayfası
     - Blockchain kayıt bilgileri
  7. Durum bildirimi:
     - Protokol modülüne geri bildirim
     - İşlem raporları
     - Hata/başarı durumları
- **Sonuç:** Sertifikalar NFT olarak basılır ve öğrencilere dağıtılır.

## Öğrenci Modülü Senaryoları

### 1. Kişisel Gelişim Takibi

**Senaryo:** Öğrenci, staj sürecindeki mesleki gelişimini takip edebilir.

- **Aktör:** Öğrenci
- **Ön Koşul:** Öğrenci sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Mesleki Gelişim' bölümüne gidin
  2. Gelişim kayıtları:
     - Yeni yeterlilik/beceri ekle
     - Öğrenilen yeni teknikler
     - Kullanılan ekipmanlar
     - Tamamlanan projeler
  3. İlerleme durumunu görüntüle
- **Sonuç:** Mesleki gelişim kaydedilir ve takip edilir.

### 2. Usta Öğretici Değerlendirmeleri

**Senaryo:** Öğrenci, usta öğreticisinden aldığı geri bildirimleri görüntüleyebilir.

- **Aktör:** Öğrenci
- **Ön Koşul:** Öğrenci sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Değerlendirmeler' bölümüne gidin
  2. Değerlendirme görüntüleme:
     - Aylık performans notları
     - Beceri değerlendirmeleri
     - Davranış değerlendirmeleri
     - Gelişim önerileri
  3. Geçmiş değerlendirmeleri incele
- **Sonuç:** Usta öğretici değerlendirmeleri görüntülenir.

### 3. Hedef Belirleme ve Takip

**Senaryo:** Öğrenci, staj sürecindeki hedeflerini belirleyip takip edebilir.

- **Aktör:** Öğrenci
- **Ön Koşul:** Öğrenci sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'Hedeflerim' bölümüne gidin
  2. Hedef yönetimi:
     - Yeni hedef oluştur
     - Hedef türünü seç
     - Tamamlanma kriterlerini belirle
     - Zaman planı oluştur
  3. Hedef takibi:
     - İlerleme durumu
     - Tamamlanan hedefler
     - Devam eden hedefler
- **Sonuç:** Mesleki hedefler belirlenir ve takip edilir.

### 4. İşletme Değerlendirme

**Senaryo:** Öğrenci, staj yaptığı işletmeyi değerlendirebilir.

- **Aktör:** Öğrenci
- **Ön Koşul:** Öğrenci sisteme giriş yapmış olmalıdır.
- **Adımlar:**
  1. 'İşletme Değerlendirme' bölümüne gidin
  2. Değerlendirme yapın:
     - Çalışma ortamı
     - Eğitim imkanları
     - Usta öğretici yaklaşımı
     - İş güvenliği
     - Genel memnuniyet
  3. Geri bildirim ekleyin
  4. 'Gönder' butonuna tıklayın
- **Sonuç:** İşletme değerlendirmesi kaydedilir.

## ~~Protokol ve Eğitim Yönetimi Modülü Senaryoları~~ (Phase 2)

### 1. Eğitim Protokolü Oluşturma

**Senaryo:** Kurum yönetimi, özel sektör kurumlarıyla eğitim protokolü oluşturabilir.

- **Aktör:** Kurum Yöneticisi
- **Ön Koşul:**
  - Yönetici sisteme giriş yapmış olmalıdır
  - Protokol yönetimi yetkisine sahip olmalıdır
- **Adımlar:**
  1. 'Protokol Yönetimi' bölümüne gidin
  2. Protokol işlemleri:
     - Yeni Protokol:
       - İşbirliği yapılacak kurumu seçin
       - Protokol detaylarını girin:
         - Protokol süresi
         - Eğitim içerikleri
         - Eğitmen bilgileri
         - Sertifikasyon detayları
         - Kontenjan bilgileri
       - Protokol dokümanını yükleyin
       - İmza sürecini başlatın
     - Protokol Takibi:
       - Onay durumu
       - İmza aşamaları
       - Yürürlük tarihleri
  3. 'Kaydet' butonuna tıklayın
- **Sonuç:** Eğitim protokolü oluşturulur ve takip edilir.

### 2. Eğitim Programı Yönetimi

**Senaryo:** Protokol kapsamındaki eğitim programları yönetilebilir.

- **Aktör:** Program Koordinatörü
- **Ön Koşul:** Koordinatör yetkisi verilmiş olmalıdır
- **Adımlar:**
  1. 'Eğitim Programları' bölümüne gidin
  2. Program işlemleri:
     - Program Oluşturma:
       - Eğitim başlığı
       - Tarih ve süre
       - Eğitmen ataması
       - Öğrenci kontenjanı
     - İçerik Yönetimi:
       - Müfredat tanımlama
       - Kaynak dökumanlar
       - Değerlendirme kriterleri
  3. Program takibi:
     - Katılım durumu
     - Başarı ölçütleri
     - Devam zorunluluğu
- **Sonuç:** Eğitim programı detayları belirlenir.

### 3. Eğitim Sertifikasyon Yönetimi (Phase 2)

**Senaryo:** Eğitim tamamlama sertifikaları için NFT dönüşüm talebi oluşturulur.

- **Aktör:** Program Koordinatörü
- **Ön Koşul:**
  - Koordinatör yetkisi
- **Adımlar:**
  1. 'Sertifikasyon' bölümüne gidin
  2. Sertifika hazırlık işlemleri:
     - Toplu Sertifika Hazırlığı:
       - Programı seçin
       - Başarılı öğrencileri listeleyin
       - Sertifika şablonu seçin
       - Sertifika bilgilerini hazırlayın:
         - Program detayları
         - Kazanılan yetkinlikler
         - Geçerlilik süresi
  3. NFT dönüşüm talebi:
     - 'NFT Dönüşüm Talebi Oluştur' butonuna tıklayın
     - Talep durumunu takip edin:
       - Hazırlanıyor
       - NFT Sistemine Aktarıldı
       - İşlem Tamamlandı
       - Hata Durumu
  4. Süreç takibi:
     - Blockchain işlem durumu
     - Dönüşüm aşamaları
     - Transfer durumları
- **Sonuç:** Sertifika bilgileri NFT dönüşümü için blockchain modülüne aktarılır.

### 4. Eğitim Değerlendirme ve Raporlama

**Senaryo:** Eğitim programlarının etkinliği değerlendirilir ve raporlanır.

- **Aktör:** Program Koordinatörü
- **Ön Koşul:** Koordinatör yetkisi
- **Adımlar:**
  1. 'Program Değerlendirme' bölümüne gidin
  2. Değerlendirme araçları:
     - Öğrenci anketleri
     - Eğitmen geri bildirimleri
     - Başarı istatistikleri
  3. Raporlama:
     - Program bazlı raporlar
     - Katılım analizleri
     - Başarı grafikleri
     - Maliyet/fayda analizi
  4. İyileştirme önerileri
- **Sonuç:** Program etkinliği ölçülür ve raporlanır.
