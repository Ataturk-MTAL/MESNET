---
title: Proje Kapsamı
---

# MESNET Proje Kapsam Tanımı

## Phase 1 - Çekirdek Staj Süreçleri

Phase 1'in amacı mesleki eğitim staj süreçlerinin uçtan uca dijitalleştirilmesidir.
Blockchain, NFT ve sertifikasyon süreçleri bu fazda kapsam dışıdır.

### Ana Odak Alanları

#### 1. Sözleşme Yönetimi

- Öğrenci-işletme staj sözleşmesi oluşturma
- İmza süreci takibi (kurum, işletme, öğrenci/veli)
- Sözleşme durumları: Taslak, İmza Bekliyor, Aktif, Askıda, Feshedilmiş, Tamamlanmış
- Sözleşme belgeleri ve şablon yönetimi

#### 2. Devamsızlık Takibi

- İşletme tarafından devamsızlık kaydı oluşturma
- Öğretmen tarafından devamsızlık girişi ve doğrulama
- Öğrenci tarafından devamsızlık görüntüleme
- Kurum tarafından devamsızlık raporlama ve yönetim
- Sağlık raporu yükleme ve devamsızlık ilişkilendirme

#### 3. Dekont ve Maaş Takibi

- İşletme tarafından maaş dekontu yükleme
- Öğretmen tarafından dekont onayı
- Kurum tarafından dekont takip ve nihai onay
- Öğrenci tarafından maaş görüntüleme ve onay
- Asgari ücret parametresi yönetimi
- Ödeme listesi oluşturma

#### 4. Staj Fesih İşlemleri

- Fesih nedeni seçimi ve gerekçe belgeleme
- Fesih türleri: öğrenci talebi, işletme talebi, disiplin, sağlık, devamsızlık aşımı, işletme kapanması
- Fesih onay süreci
- Fesih sonrası yeni işletmeye yerleşme sürecine yönlendirme

#### 5. Yeni İşletmeye Yerleşme

- Stajsız öğrenci havuzu yönetimi
- Alan/dal bazlı işletme eşleştirme
- Kontenjan kontrolü
- Lokasyon bazlı filtreleme
- Yeni sözleşme sürecini başlatma

#### 6. Lokasyon Bazlı İşletme Yönetimi

- İşletme konum kaydı (harita üzerinden veya adres ile)
- Yakın işletme arama (mesafe yarıçapı, alan/dal filtresi)
- Harita ve liste görünümü
- Öğretmen ziyaret rotası planlama ve optimizasyonu
- Konum bazlı raporlama

#### 7. İşletme Belge Yönetimi

- Ustalık belgesi yükleme ve takip
- Usta öğreticilik belgesi yükleme ve takip
- Belge onay süreci (kurum tarafından)
- Belge geçerlilik tarihi kontrolü
- Eksik/süresi dolan belge bildirimleri

#### 8. Öğretmen Koordinatörlük Süreçleri

- Öğrenci ve işletme listeleme
- İşletme ziyaret programı oluşturma (ders programı formatında)
- Karekodlu ziyaret raporu yazdırma
- Evrak teslim takibi (karekod veya manuel)
- Devamsızlık ve dekont işlemleri

### Phase 1 Modülleri (Domain/Capability Bazlı)

> Modüller aktör bazlı değil, domain capability bazlı bölünmüştür.
> Detaylı tasarım: `ModuleDesign.md`

| Modül | Bounded Context | Storage |
|-------|----------------|---------|
| Business | İşletme kaydı, belgeleri, konum, kontenjan yönetimi | Document |
| Enrollment | Öğrenci-işletme eşleştirme, başvuru, yerleştirme | Hybrid |
| Contract | Staj sözleşmesi yaşam döngüsü, imza, fesih | Event Sourcing |
| Attendance | İşyeri devamsızlık kaydı ve takibi | Event Sourcing |
| Payment | Maaş hesaplama, dekont yükleme ve onay süreci | Event Sourcing |
| Coordination | Öğretmen ziyaret programı, rapor, evrak takibi | Hybrid |
| Institution | Kurum bilgileri, personel, alan/dal, parametreler | Document |
| Internship | Staj yaşam döngüsü orkestrasyonu (saga) | Saga + Projection |
| Reporting | Denormalize veri, QuestPDF ile PDF rapor üretimi | Document (denormalize) |

### Phase 1 Aktörleri

- Kurum Müdürü
- Müdür Yardımcısı
- Program Koordinatörü
- Koordinatör Öğretmen
- İşletme Yöneticisi
- Stajyer Öğrenci

---

## Phase 2 - Blockchain ve Sertifikasyon (Phase 1 sonrası)

Phase 2, Phase 1 tamamlanıp stabil hale geldikten sonra ele alınacaktır.

### Kapsam

**Tenant (Çoklu Kurum Yönetimi):**

- Çoklu kurum yönetimi ve sistem geneli yapılandırma
- Tenant Yöneticisi aktörü ve yetkileri
- Çapraz kurum raporları (RCR)
- Kurum ekleme/düzenleme (Tenant üzerinden)

**Blockchain ve Sertifikasyon:**

- Blockchain altyapısı ve smart contract yönetimi
- NFT tabanlı sertifika sistemi
- Web3 cüzdan entegrasyonu
- Sertifika basım ve transfer süreçleri
- Gas optimizasyonu
- Protokol ve eğitim yönetimi modülü
- Eğitim sertifikasyon süreçleri

### Phase 2 Ek Aktörleri

- Tenant Yöneticisi
- Blockchain Sistem Yöneticisi
- Doğrulayıcı (İşveren/Kurum)

### Phase 2 Ek İzinleri

- `blockchain:*` (view, mint, deploy, manage, monitor)
- `certificate:*` (view, prepare, approve, mint, validate)
