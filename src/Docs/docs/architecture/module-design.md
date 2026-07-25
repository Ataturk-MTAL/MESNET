---
title: Modül Tasarımı
---

# MESNET Modül Tasarımı — Domain/Capability Bazlı Yaklaşım

## Tasarım Felsefesi

Modüller **aktör bazlı** (Student, Teacher, Business) değil, **domain capability bazlı** bölünmüştür.
Her modül bir **bounded context** temsil eder ve bağımsız olarak microservice'e dönüştürülebilecek şekilde tasarlanmıştır.

### Neden Aktör Bazlı Değil?

Aktör bazlı modülleme (Student modülü, Teacher modülü gibi) şu sorunlara yol açar:

- **Aynı aggregate birden fazla modüle dağılır.** Örneğin "devamsızlık" kavramı hem işletme, hem öğrenci, hem öğretmen, hem kurum modülünde yer alır.
- **Modüller arası aşırı event bağımlılığı** oluşur.
- **Veri duplikasyonu** kaçınılmaz hale gelir.
- **Domain sınırları** bulanıklaşır.

Domain/capability bazlı yaklaşımda her modül **bir iş yeteneğine** sahiptir ve tüm aktörler ilgili yetenekle etkileşime geçer.

## Mimari Temeller: CQRS + Event Sourcing

MESNET, **CQRS (Command Query Responsibility Segregation)** ve **Event Sourcing** mimarisini baz alan bir **modüler monolit** uygulamadır. Bu mimari seçimlerinin temel nedenleri:

### CQRS — Komut/Sorgu Ayrımı

Tüm işlemler **yazma (command)** ve **okuma (query)** olarak kesin biçimde ayrılır:

- **Command handler'lar** — Durum değiştiren işlemler. Event yayınlar, hata durumunda `DomainException` fırlatır. `IDocumentSession` (yazma oturumu) kullanır.
- **Query handler'lar** — Salt okuma işlemleri. Hiçbir side effect oluşturmaz. `IQuerySession` (okuma oturumu) kullanır.
- **Endpoint'ler** — İnce HTTP adaptör katmanıdır; iş mantığı içermez, tüm işlemi Wolverine handler'a devreder (`bus.InvokeAsync`).

Bu ayrım sayesinde okuma ve yazma bağımsız olarak ölçeklenebilir, test edilebilir ve optimize edilebilir.

### Event Sourcing — Olay Kaynağı

Durum geçişleri kritik olan entity'ler (sözleşmeler, devamsızlık, maaş, dağıtım onayları) **event sourcing** ile yönetilir:

- Entity'nin güncel durumu, geçmiş event'lerin sırasıyla yeniden oluşturulur (replay).
- Her durum değişikliği bir event olarak saklanır → tam audit trail.
- **Marten Event Store** üzerinden PostgreSQL'de `mt_streams` ve `mt_events` tablolarında tutulur.
- **Snapshot (Inline Projection)** ile güncel durum otomatik olarak materialized view'da tutulur → her okumada replay gerekmez.
- **Decider pattern** (`[AggregateHandler]`) ile event sourcing aggregate'leri yönetilir.

### Document Storage — Belge Saklama

CRUD ağırlıklı entity'ler (işletme bilgileri, kurum bilgileri, öğrenci profilleri) **Marten document storage** ile yönetilir:

- .NET nesneleri PostgreSQL'e JSONB olarak serialize edilir.
- LINQ ile sorgulama desteklenir.
- Geleneksel RDBMS + ORM yaklaşımına kıyasla daha az boilerplate, daha hızlı geliştirme.

### Hybrid Yaklaşım

Çoğu modül her iki pattern'ı birlikte kullanır. Örneğin Business modülünde:

- İşletme bilgileri → document storage (CRUD)
- İşletme durum geçişleri → event sourcing (audit trail)

Hangi entity'nin hangi pattern'ı kullandığı, her modülün "Storage tipi" bölümünde belirtilmiştir.

### Teknoloji Yığını Özeti

| Katman | Teknoloji | Açıklama |
| ------ | --------- | -------- |
| CQRS / Message Bus | **Wolverine** | Command/query dispatching, cascading messages, saga, durable local queues |
| Document DB + Event Store | **Marten** | PostgreSQL üzerinde JSONB document storage + event sourcing |
| HTTP API | **ASP.NET Minimal API** | Endpoint'ler `MapGet`/`MapPost` ile tanımlanır, Wolverine'e devreder |
| Kimlik Doğrulama | **Keycloak** | OAuth2/OIDC, PKCE flow |
| Frontend | **Quasar (Vue 3 + TypeScript)** | SPA, Pinia state management |

## Modül Yapısı

### 0. Security (Kimlik Doğrulama ve Kullanıcı Yönetimi)

**Bounded Context:** Kullanıcı hesabı yönetimi, Keycloak entegrasyonu, rol ve doğrudan izin atama, kullanıcı davet akışı. Tüm modüllerin temel bağımlılığıdır — `UserCreated` event'i Institution, Enrollment ve Business modüllerinde ilgili profil kayıtlarının oluşmasını tetikler.

**Aggregate Roots:**
- `UserAccount` — Keycloak kullanıcısının yerel gölge kopyası (Keycloak ID, roller, doğrudan izinler, kurum/işletme/öğrenci bağı)
- `UserInvitation` — Davet kaydı ve onay akışı durumu (token, hedef rol, metadata, geçerlilik)

**Sorumluluklar:**
- Keycloak Admin API ile kullanıcı oluşturma, güncelleme, aktif/pasif yapma, silme
- Realm rolü atama ve değiştirme
- Doğrudan izin (DirectPermissions) atama — Keycloak attribute + yerel gölge kopya
- Kullanıcı davet süreci yönetimi (Onay Bekliyor → Onaylandı → Tamamlandı / Reddedildi / Süresi Doldu)
- Davet e-postası gönderme ve yeniden gönderme
- Kullanıcı ve davet listeleme, arama, sayfalama
- Rol/izin değişiminde yetki claim cache invalidation (`PermissionClaimsTransformation`)

**Kimlik Doğrulama Altyapısı:**
- Keycloak (OAuth2 / OIDC, PKCE flow) — frontend public client, API confidential client
- Realm rolleri (6): `InstitutionManager`, `InstitutionStaff`, `Teacher`, `Student`, `DepartmentHead`, `CompanyManager` (`MESNET.Common.Shared/Security/MesnetRoles.cs`)
- Custom user attributes: `institution_id`, `business_id`, `direct_permissions`
- İzin sabitleri `MESNET.Common.Shared/Security/Permissions.cs`'te tanımlı; Keycloak rolleri + doğrudan izinler `PermissionClaimsTransformation` (`MESNET.Common.Infrastructure`) ile JWT claim'lerine çevrilir

**Davet Akışı:** `CreateInvitation` (davet oluştur) → `ApproveInvitation` (onayla; +7 gün geçerlilik + e-posta) → `CompleteInvitation` (Keycloak'a kullanıcı oluştur, rol/attribute ata, `UserAccount` kaydet) → `InvitationCompleted` + `UserCreated` cascade event'leri.

**Storage tipi:** Document (CRUD ağırlıklı, event sourcing kullanılmaz)

**Publish ettiği eventler:**
- `UserCreated` — Institution / Enrollment / Business tarafından consume edilir (profil oluşturma)
- `UserUpdated`
- `UserRolesChanged`
- `UserPermissionsChanged`
- `UserActivated`
- `UserDeactivated`
- `UserDeleted`
- `InvitationCreated`
- `InvitationApproved`
- `InvitationRejected`
- `InvitationCompleted`

**Dinlediği eventler:** Yok — Phase 1'de tek yönlü olay akışının kaynağıdır (başka modülün event'ini consume etmez).

---

### 1. Business (İşletme Yönetimi)

**Bounded Context:** İşletme kaydı, onay süreci, durum yönetimi, belgeleri ve konum yönetimi.

**Aggregate Roots:**
- `Business` — İşletme bilgileri, konum, belgeler, durum geçişleri
- `BusinessRepresentative` — İşletme yetkilileri

**Sorumluluklar:**
- İşletme kaydı (kurum veya işletme kendi kaydı — kendi kayıtta kurum onayı zorunlu)
- İşletme onay süreci (ONAY_BEKLİYOR → AKTİF / REDDEDİLDİ)
- İşletme durum yönetimi (AKTİF / PASİF / KAPATILMIŞ — event sourcing ile geçmiş korunur)
- İşletme belge yönetimi (ustalık, usta öğreticilik belgeleri)
- Lokasyon kaydı ve güncelleme
- Yakın işletme arama
- Kontenjan yönetimi
- İşletme fesih talebi (gerekçeli, Contract/Internship'e iletilir)

**İş Kuralları:** Detaylar `./business-rules.md` dosyasında

**Storage tipi:** Hybrid — İşletme bilgileri document, durum geçişleri event sourcing

**Publish ettiği eventler:**
- `BusinessRegistered`
- `BusinessUpdated`
- `BusinessApprovalRequested` — İşletme kendi kaydında kurum onayı bekliyor
- `BusinessApproved` — Kurum onayladı
- `BusinessRejected` — Kurum reddetti
- `BusinessDeactivated` — İşletme pasife alındı
- `BusinessClosed` — İşletme kalıcı kapatıldı
- `BusinessDocumentUploaded`
- `BusinessDocumentApproved`
- `BusinessCapacityChanged`
- `BusinessTerminationRequested` — İşletme gerekçeli fesih talebi

---

### 2. Enrollment (Kayıt ve Eşleştirme)

**Bounded Context:** Öğrenci-işletme eşleştirme, staj başvurusu, yerleştirme.

**Aggregate Roots:**
- `StudentProfile` — Öğrenci staj profili (alan, dal, tercihler)
- `InternshipPlacement` — Öğrenci-işletme eşleşmesi (event sourced)

**Sorumluluklar:**
- Öğrenci kayıt işlemleri
- Öğretmen kayıt işlemleri
- Staj başvurusu oluşturma
- İşletme öğrenci talep etme
- Öğrenci-işletme eşleştirme
- Yeni işletmeye yerleştirme
- Lokasyon bazlı eşleştirme

**Storage tipi:** Hybrid — StudentProfile document, InternshipPlacement event sourced

**Publish ettiği eventler:**
- `StudentRegistered`
- `TeacherRegistered`
- `InternshipApplied`
- `StudentPlaced`
- `StudentTransferred`

---

### 3. Contract (Sözleşme Yönetimi)

**Bounded Context:** Staj sözleşmesi yaşam döngüsü.

**Aggregate Roots:**
- `InternshipContract` — Sözleşme (event sourced)

**Durumlar:** Taslak → İmza Bekliyor → Aktif → Askıda → Feshedilmiş / Tamamlanmış

**Sorumluluklar:**
- Sözleşme oluşturma
- İmza süreci takibi (kurum, işletme, öğrenci/veli)
- Sözleşme aktivasyonu
- Sözleşme fesih işlemi (nedenler, gerekçe, belgeler)
- Sözleşme tamamlama

**Storage tipi:** Event Sourcing (durum geçişleri kritik)

**Publish ettiği eventler:**
- `ContractCreated`
- `ContractSignedByInstitution`
- `ContractSignedByBusiness`
- `ContractSignedByStudent`
- `ContractActivated`
- `ContractSuspended`
- `ContractTerminated`
- `ContractCompleted`

---

### 4. Attendance (Devamsızlık Takibi)

**Bounded Context:** İşyeri devamsızlık kaydı ve takibi.

**Aggregate Roots:**
- `AttendanceRecord` — Devamsızlık kaydı (event sourced)

**Sorumluluklar:**
- İşletme tarafından devamsızlık kaydı oluşturma (resmi tatil ve okul izin günleri hariç)
- Öğretmen tarafından doğrulama
- Kurum tarafından yönetim ve raporlama
- Öğrenci tarafından görüntüleme
- Sağlık raporu ilişkilendirme
- Devamsızlık limiti kontrolü
- İş takvimi yönetimi (resmi tatiller + okul izin günleri)

**İş Kuralları:** Devamsızlık gün kısıtlamaları ve limit kuralları `./business-rules.md` dosyasında

**Storage tipi:** Event Sourcing (kimin ne zaman kayıt girdiği, onayladığı önemli)

**Publish ettiği eventler:**
- `AttendanceMarked`
- `AttendanceVerified`
- `AttendanceCorrected`
- `HealthReportAttached`
- `AttendanceLimitExceeded`

---

### 5. Payment (Maaş ve Dekont)

**Bounded Context:** Maaş hesaplama, dekont yükleme ve onay süreci.

**Aggregate Roots:**
- `SalaryPeriod` — Aylık maaş dönemi (event sourced)
- `FeeReceipt` — Dekont kaydı (event sourced)

**Sorumluluklar:**
- Asgari ücret parametresi yönetimi
- 3308 Madde 25'e göre maaş hesaplama (personel sayısına göre %15/%30/%50 oranları)
- Devlet katkısı hesaplama (<20 personel → 2/3, ≥20 personel → 1/3, MEM → tam)
- Devamsızlık kesintisi hesaplama (formül: AylıkÜcret/30 × DevamsızGün)
- İşletme tarafından dekont yükleme
- Öğretmen tarafından dekont kontrolü ve onayı
- Kurum tarafından nihai onay
- Öğrenci tarafından maaş onayı
- Ödeme listesi oluşturma

**İş Kuralları:** Maaş hesaplama formülleri ve devlet katkısı kuralları `./business-rules.md` dosyasında

**Storage tipi:** Event Sourcing (onay zinciri, audit trail kritik)

**Publish ettiği eventler:**
- `SalaryCalculated`
- `ReceiptUploaded`
- `ReceiptApprovedByTeacher`
- `ReceiptApprovedByInstitution`
- `ReceiptRejected`
- `SalaryConfirmedByStudent`
- `PaymentListGenerated`

---

### 6. Coordination (Öğretmen Koordinatörlük)

**Bounded Context:** Öğretmen-işletme ziyaret programı, ders programı yönetimi, alan bazlı işletme dağıtımı, iş yükü hesaplama, raporlama, evrak takibi.

**Aggregate Roots:**
- `VisitSchedule` — Ziyaret programı (document)
- `VisitReport` — Ziyaret raporu (event sourced)
- `DepartmentDistribution` — Alan bazlı işletme dağıtımı (event sourced — onay zinciri audit trail)

**Document'lar:**

- `TeacherSchedule` — Öğretmen haftalık ders programı (5 gün × N ders saati, Occupied/Free)
- `CoordinationConfig` — Kurum başına koordinatörlük ayarları (mesafe-saat kuralları, azami haftalık saat)
- `BusinessCoordinationView` — İşletme-öğretmen atama read model (denormalize: mesafe, saat, alan bilgisi)

**Sorumluluklar:**

- Öğretmen-işletme atama (alan bazlı, mesafe-saat formülü)
- Haftalık ziyaret programı oluşturma (ders programı formatı)
- Rota bazlı mesafe hesaplama (OSRM — gerçek yol mesafesi, Haversine fallback)
- Karekodlu rapor oluşturma ve yazdırma
- Evrak teslim takibi (karekod veya manuel)
- Koordinatör-öğrenci iletişimi
- Öğretmen ders programı yönetimi (boş saat tespiti → işletme atama havuzu)
- Alan bazlı işletme dağıtımı (zümre karar tutanağı + müdür onayı)
- İş yükü hesaplama (mesafe-saat formülü, toplam takdir edilen ≤ toplam verilebilir kısıtı)
- Dağıtım onay süreci (TASLAK → ZÜMRE_KARARI_ALINDI → MÜDÜR_ONAY_BEKLİYOR → ONAYLANDI / REDDEDİLDİ)

**İş Kuralları:** Ek ders ve görevlendirme kuralları `./business-rules.md` Bölüm 11'de

**Harici Servis:** OSRM (Open Source Routing Machine) — Podman container, Türkiye OpenStreetMap verisi, rota bazlı mesafe hesaplama

**Storage tipi:** Hybrid — VisitSchedule, TeacherSchedule, CoordinationConfig, BusinessCoordinationView document; VisitReport, DepartmentDistribution event sourced

**Publish ettiği eventler:**
- `TeacherAssignedToBusiness`
- `VisitScheduleCreated`
- `VisitCompleted`
- `ReportGenerated`
- `DocumentDelivered`
- `DepartmentDistributionCreated`
- `DepartmentDistributionApprovedByPrincipal`
- `DepartmentDistributionRejected`
- `TeacherScheduleUpdated`
- `TeacherWorkloadCalculated`

**Dinlediği eventler:**

- `StudentTransferred` (Enrollment) → öğretmen-işletme ataması güncellenir
- `ContractTerminated` (Contract) → öğretmen-işletme ataması güncellenir
- `StaffAuthorized` (Institution, role=ALAN_SEFI) → alan şefi bilgisi alınır

---

### 7. Institution (Kurum Yönetimi)

**Bounded Context:** Kurum bilgileri, personel yönetimi, MEB alan/dal kataloğu, kurum alan/dal aktivasyonu, sistem ayarları.

**Aggregate Roots:**

- `Institution` — Kurum bilgileri, kurum alan/dal atamaları (document)

**Document'lar:**

- `FieldOfStudy` — MEB resmi alan/dal kataloğu (sistem geneli referans data, seed data ile yüklenir)

**Value Object'ler:**

- `Specialization` — Dal tanımı (FieldOfStudy içinde)
- `InstitutionBranch` — Kurumun aktifleştirdiği alan/dal bilgisi (Institution aggregate içinde)
- `Location` — Konum koordinatları

**Sorumluluklar:**

- Kurum bilgileri CRUD
- Personel yetkilendirme (roller: MUDUR, MUDUR_YARDIMCISI, KOORDINATOR, ALAN_SEFI, PERSONEL)
- MEB resmi alan/dal kataloğu yönetimi (FieldOfStudy — MTAL 56 alan/119 dal, MESEM 39 alan/193 dal)
- Kurum bazlı alan/dal aktifleştirme ve pasifleştirme (MEB kataloğundan seçim)
- Kurumdaki aktif alanın dal seçimini güncelleme
- Sistem parametreleri (asgari ücret vb.)
- Alan şefi atama (StaffRole.ALAN_SEFI + BranchCode ile hangi alandan sorumlu olduğu)

**Storage tipi:** Document (CRUD ağırlıklı)

**Seed Data:** FieldOfStudy document'ları Marten InitialData ile uygulama başlangıcında yüklenir:

- `mtal_fields.json` — 56 alan, 119 dal (EducationType = ORGUN)
- `mesem_fields.json` — 39 alan, 193 dal (EducationType = MESEM)

**Publish ettiği eventler:**

- `InstitutionUpdated`
- `StaffAuthorized` — Coordination modülü dinler (ALAN_SEFI rolünde alan şefi bilgisi alınır)
- `MinimumWageUpdated`
- `BranchActivated` — Kurum bir alanı aktifleştirdiğinde
- `BranchDeactivated` — Kurum bir alanı pasifleştirdiğinde
- `BranchSpecializationsUpdated` — Kurumdaki bir alanın aktif dalları güncellendiğinde

---

### 8. Tenant (Üst Yönetim) — ⚠️ PHASE 2

> **Bu modül Phase 2'ye ertelenmiştir.** Phase 1'de tek kurum senaryosu ile çalışılacak. Çoklu kurum desteği gerektiğinde bu modül aktifleştirilecektir.

**Bounded Context:** Çoklu kurum yönetimi, sistem geneli yapılandırma.

**Aggregate Roots:**
- `Tenant` — Tenant bilgileri (document)

**Sorumluluklar:**
- Kurum ekleme/düzenleme
- Kurum müdürü hesapları yönetimi
- Sistem geneli istatistik ve raporlama
- Çapraz kurum raporları

**Storage tipi:** Document

---

### 9. Internship (Staj Orkestrasyon — Saga)

**Bounded Context:** Staj yaşam döngüsü orkestrasyonu. Kendi domain verisi YOKTUR; diğer modüllerin event'lerini dinleyerek çapraz modül iş akışlarını koordine eder.

**Wolverine Saga:**
- `InternshipSaga` — Staj yaşam döngüsünü orkestre eden Wolverine saga

**Composite Read Model (Projection):**
- `InternshipSummary` — Tüm modüllerden gelen event'leri dinleyerek oluşturulan birleşik staj durumu görünümü

**Sorumluluklar:**
- Staj yaşam döngüsü orkestrasyonu (kayıt → sözleşme → aktif staj → tamamlama/fesih)
- Fesih onay zinciri koordinasyonu (öğrenci yaşına göre veli onayı, koordinatör, müdür yardımcısı, müdür)
- Devamsızlık limiti aşımında otomatik fesih sürecini tetikleme
- Fesih sonrası yeni yerleştirme sürecini başlatma
- Sözleşme aktivasyonu sonrası devamsızlık ve maaş takibini başlatma
- Cross-module durum sorgulama (InternshipSummary projection üzerinden)

**Storage tipi:** Hybrid — Saga state Wolverine tarafından yönetilir, InternshipSummary Marten async projection

**Dinlediği eventler:**
- `StudentPlaced` (Enrollment) → saga başlatır
- `ContractActivated` (Contract) → stajı aktif olarak işaretler
- `ContractTerminated` (Contract) → fesih sonrası yeni yerleştirme sürecini koordine eder
- `ContractCompleted` (Contract) → stajı tamamlanmış olarak işaretler
- `AttendanceLimitExceeded` (Attendance) → fesih sürecini tetikler
- `SalaryConfirmedByStudent` (Payment) → maaş onay durumunu günceller
- `VisitCompleted` (Coordination) → ziyaret durumunu günceller

**Publish ettiği eventler:**
- `InternshipStarted`
- `InternshipCompleted`
- `InternshipTerminationRequested`
- `InternshipTerminationApprovalChainStarted`
- `InternshipReplacementRequested`

---

### 10. Reporting (Raporlama)

**Bounded Context:** Tüm modüllerden gelen verilerin denormalize edilmesi ve PDF rapor üretimi.

**Denormalize Read Model'ler (Marten Document):**

- `StudentReportView` — Öğrenci bazlı birleşik rapor verisi
- `BusinessReportView` — İşletme bazlı birleşik rapor verisi
- `AttendanceReportView` — Devamsızlık rapor verisi
- `PaymentReportView` — Maaş/dekont rapor verisi
- `InstitutionReportView` — Kurum bazlı istatistik verisi
- `TerminationReportView` — Fesih rapor verisi
- `CoordinatorDailyFormView` — Koordinatör günlük form verisi (ziyaret bazlı yoklama)
- `CoordinatorMonthlyFormView` — Koordinatör aylık form verisi (özet)
- `AttendanceSheetView` — Devamsızlık çizelgesi (ay × öğrenci matrisi)

**Sorumluluklar:**

- Tüm modül event'lerini dinleyerek denormalize rapor veritabanını güncel tutma
- QuestPDF ile PDF rapor üretimi (devamsızlık listeleri, maaş listeleri, staj durumu raporları vb.)
- Kurum, öğretmen, işletme bazlı raporlar
- Dönemsel istatistik raporları
- Toplu liste çıktıları (Excel/PDF)
- Koordinatör günlük form PDF üretimi (ziyaret yoklaması, gözlem notları)
- Koordinatör aylık form PDF üretimi (devam durumu, maaş, sözleşme özeti)
- Devamsızlık çizelgesi PDF üretimi (gün bazlı durum kodları: V/Y/M/S/T)
- Fesih formu PDF üretimi (ıslak imza alanları)

**Teknoloji:**
- **PDF Engine:** QuestPDF (https://www.questpdf.com/)
- **Storage:** Marten Document Store (kendi `reporting` schema'sı)
- Tüm veriler denormalize document olarak saklanır
- Diğer modüllerin DB'sine ASLA doğrudan erişmez

**Dinlediği eventler:**

- Tüm modüllerden gelen public event'ler (StudentRegistered, ContractActivated, AttendanceMarked, ReceiptUploaded, vb.)
- Coordination event'leri (VisitCompleted, TeacherAssignedToBusiness, DepartmentDistributionApprovedByPrincipal) → günlük/aylık form view'ları güncellenir
- Attendance event'leri (AttendanceMarked, AttendanceVerified) → devamsızlık çizelgesi güncellenir
- Internship termination event'leri → fesih rapor view'ı güncellenir
- Her event geldiğinde ilgili denormalize document güncellenir

**Publish ettiği eventler:**

- `ReportRequested`
- `ReportGenerated`
- `TerminationReportGenerated`

---

## Modüller Arası İletişim Kuralları

```
                    ┌──────────────┐
                    │   Tenant     │  ← Phase 2
                    └──────┬───────┘
                           │ events (Phase 2)
                    ┌──────▼───────┐
                    │ Institution  │
                    └──────┬───────┘
                           │ events
          ┌────────────────┼────────────────┐
          │                │                │
   ┌──────▼──────┐  ┌─────▼──────┐  ┌──────▼──────┐
   │  Business   │  │ Enrollment │  │Coordination │
   └──────┬──────┘  └─────┬──────┘  └──────┬──────┘
          │               │                │
          │         ┌─────▼──────┐         │
          │         │  Contract  │         │
          │         └─────┬──────┘         │
          │               │                │
          ├───────┬───────┼────────────────┘
          │       │       │
   ┌──────▼──┐ ┌──▼──────▼──┐
   │Attendance│ │  Payment   │
   └────┬────┘ └─────┬──────┘
        │            │
        │  ┌─────────▼──────────┐
        │  │    Internship      │  ← saga/orchestrator
        │  │  (tüm event'leri   │    (kendi domain verisi yok)
        └──►   dinler)          │
           └─────────┬──────────┘
                     │ tüm event'ler
           ┌─────────▼──────────┐
           │    Reporting       │  ← denormalize DB
           │  (QuestPDF + tüm   │    (PDF rapor üretimi)
           │   event'leri dinler)│
           └────────────────────┘
```

### Kurallar

1. **Modüller arası iletişim yalnızca Wolverine events ile yapılır** (`PublishAsync`)
2. **Senkron çağrı (`InvokeAsync`) modüller arasında YASAKTIR**
3. **Her modül sadece kendi Shared projesindeki event'leri publish eder**
4. **Bir modül başka modülün Shared projesini consume edebilir** (read-only dependency)
5. **Cross-module veri okuma**: Her modül ihtiyaç duyduğu veriyi kendi projection'ı ile oluşturur (event'leri dinleyerek), doğrudan başka modülün DB'sine sorgu ATMAZ
6. **Eventual consistency kabul edilir** modüller arasında

### Event Flow Örnekleri

**Staj fesih ve yeni yerleşme:**
```
Contract → ContractTerminated event
  → Enrollment dinler → öğrenciyi "yerleştirilecek" havuzuna ekler
  → Attendance dinler → devamsızlık kayıtlarını kapatır
  → Payment dinler → maaş dönemini keser
  → Coordination dinler → ziyaret programından çıkarır
```

**Devamsızlık limiti aşımı:**
```
Attendance → AttendanceLimitExceeded event
  → Contract dinler → sözleşme fesih sürecini başlatır
```

**Dekont onay zinciri:** (sıra zorunlu — bkz. business-rules.md §6.6)
```
Payment → ReceiptUploadedByBusiness (işletme dekontu yükler)
  → SalaryConfirmedByStudent (öğrenci parayı aldığını onaylar)
  → ReceiptApprovedByTeacher (koordinatör öğretmen onaylar)
  → ReceiptApprovedByDeputy (müdür yardımcısı son onay) → PaymentCompleted
```

**Internship saga — staj yaşam döngüsü:**
```
Enrollment → StudentPlaced event
  → Internship saga başlar → InternshipStarted publish eder
  → Contract → ContractActivated event
    → Internship saga stajı "aktif" olarak işaretler
  → Attendance → AttendanceLimitExceeded event
    → Internship saga fesih sürecini koordine eder
    → InternshipTerminationApprovalChainStarted publish eder
  → Contract → ContractCompleted event
    → Internship saga → InternshipCompleted publish eder
```

**Reporting denormalizasyonu:**
```
Tüm modüller → event publish eder
  → Reporting modülü dinler → ilgili denormalize document güncellenir
  → Rapor talebi geldiğinde → QuestPDF ile hazır veriden PDF üretilir
```

## PostgreSQL Schema Dağılımı ve Şema İzolasyonu

### Şema İzolasyonu İlkesi

Modüler monolitin temel taşıyıcı ilkesi **şema bazlı izolasyondur**. Her modül kendi PostgreSQL schema'sına sahiptir ve yalnızca kendi schema'sındaki tablolara erişebilir. Bu izolasyon, modüllerin bağımsızlığını garanti eder ve ileride microservice'e geçişi kolaylaştırır.

**İzolasyon kuralları:**

1. **Her modül kendi schema'sına sahiptir** — `ConfigureMarten` ile `DatabaseSchemaName` belirtilir.
2. **Bir modül başka modülün schema'sına ASLA doğrudan sorgu atamaz** — ne okuma ne yazma.
3. **Cross-module veri ihtiyacı event ile çözülür** — Kaynak modül event publish eder, hedef modül kendi schema'sında denormalize read model (projection) oluşturur.
4. **Event stream'ler paylaşımlı `shared` schema'dadır** — Marten event store tüm modüllerin event'lerini tek yerde tutar (`mt_streams`, `mt_events`).
5. **Wolverine messaging altyapısı `wolverine` schema'sındadır** — Durable outbox, dead letter queue vb.
6. **Frontend enrichment alternatifi** — Basit isim çözümleme (ID → name) gibi durumlarda backend projection yerine frontend lookup map'ler de kullanılabilir.

**Schema konfigürasyon örneği (her modülün Persistence katmanında):**

```csharp
// Coordination modülü örneği
services.ConfigureMarten(opts =>
{
    opts.Schema.For<TeacherSchedule>().DatabaseSchemaName("coordination");
    opts.Schema.For<VisitSchedule>().DatabaseSchemaName("coordination");
});
```

### Schema Tablosu

| Modül | Schema | Storage | Açıklama |
| ----- | ------ | ------- | -------- |
| Security | `security` | Document | Kullanıcı hesabı, davet, rol/izin gölge kopyası |
| Business | `business` | Document + Event | İşletme bilgileri, durum geçişleri |
| Enrollment | `enrollment` | Document + Event | Öğrenci/öğretmen profilleri, yerleştirme |
| Contract | `contract` | Event Sourcing | Sözleşme yaşam döngüsü |
| Attendance | `attendance` | Event Sourcing | Devamsızlık kaydı ve takibi |
| Payment | `payment` | Event Sourcing | Maaş/dekont onay zinciri |
| Coordination | `coordination` | Document + Event | Ders programı, ziyaret, dağıtım |
| Institution | `institution` | Document | Kurum bilgileri, alan/dal, dönem |
| Tenant *(Phase 2)* | `tenant` | Document | Çoklu kurum yönetimi |
| Internship | `internship` | Saga state + Projection | Staj orkestrasyonu |
| Reporting | `reporting` | Document (denormalize) | PDF rapor üretimi |
| Marten Event Store | `shared` | Event streams + events | Tüm modüllerin event sourcing verileri |
| Wolverine | `wolverine` | Messaging infra | Durable outbox, inbox, dead letter |

## Katman Yapısı (Her Modül)

```
MESNET.{Module}.Core/
  ├── Aggregates/        # Aggregate root'lar
  ├── Entities/          # Entity'ler
  ├── ValueObjects/      # Value object'ler
  └── Events/            # Domain events (internal)

MESNET.{Module}.Application/
  ├── Commands/          # Wolverine command handler'lar
  ├── Queries/           # Wolverine query handler'lar
  └── EventHandlers/     # Başka modüllerden gelen event handler'lar

MESNET.{Module}.Api/
  └── Endpoints/         # ASP.NET Minimal API endpoint'ler (MapGet, MapPost → bus.InvokeAsync)

MESNET.{Module}.Persistence/
  ├── MartenConfig.cs    # IConfigureMarten — schema, projection, index tanımları
  └── Projections/       # Marten projection'lar

MESNET.{Module}.Shared/
  └── Events/            # Public domain events (diğer modüllerin consume edeceği)
```

---

## Dönemsellik (Academic Period) Mimarisi

### Temel Kavram

MESNET'te tüm staj süreçleri **eğitim-öğretim dönemi** bazlı çalışır. Dönem, bir eğitim-öğretim yılını kapsar (örn. 2025-2026). Her sene başında yeni dönem oluşturulur ve yeni öğrenci listeleri bu dönem altında kayıt edilir.

**Dönem = süreç tahtası.** Dönem açık olduğu sürece her türlü işlem (kayıt, yerleştirme, sözleşme, devamsızlık, maaş vb.) yapılabilir. Dönem kapatıldığında geçmiş dönem verileri salt okunur hale gelir.

### AcademicPeriod Entity

**Sahip modül:** Institution (kurumun takvim sorumluluğu)

```csharp
public sealed class AcademicPeriod
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }
    public string Name { get; init; }           // "2025-2026"
    public int StartYear { get; init; }          // 2025
    public int EndYear { get; init; }            // 2026
    public DateOnly StartDate { get; init; }     // 2025-09-15
    public DateOnly EndDate { get; init; }       // 2026-06-15
    public AcademicPeriodStatus Status { get; set; } // Active / Closed
}
```

**Kurallar:**

- Bir kurumda aynı anda **yalnızca bir** aktif dönem olabilir
- Yeni dönem açıldığında önceki dönem otomatik **kapatılır**
- Kapatılmış döneme yazma işlemi yapılamaz (backend gate)

### Dönem Kapsamındaki Modüller

| Modül | Dönem İlişkisi | Açıklama |
|-------|---------------|----------|
| **Institution** | Dönem sahibi | `AcademicPeriod` entity'sini barındırır, `AcademicPeriodCreated` / `AcademicPeriodClosed` event'lerini publish eder |
| **Enrollment** | `AcademicPeriodId` alanı | Öğrenci kayıtları ve yerleştirmeler dönem bazlı. Her dönem yeni öğrenci listesi oluşturulur |
| **Contract** | `AcademicPeriodId` alanı | Sözleşmeler dönem bazlı. Geçmiş dönem sözleşmesi düzenlenemez |
| **Attendance** | `AcademicPeriodId` alanı | Devamsızlık kayıtları dönem bazlı |
| **Payment** | `AcademicPeriodId` alanı | Maaş/dekont süreçleri dönem bazlı |
| **Coordination** | `AcademicPeriodId` alanı | Ziyaret, sınav, rapor dönem bazlı (mevcut `AcademicYear` + `AcademicSemester` alanları korunur, ek olarak `AcademicPeriodId` eklenir) |
| **Internship** | `AcademicPeriodId` alanı | Saga state dönem bazlı |
| **Reporting** | Dönem filtreli sorgular | Denormalize view'larda `AcademicPeriodId` alanı — rapor üretiminde dönem filtresi zorunlu |
| **Business** | ❌ Dönemsellik yok | İşletmeler dönemden bağımsızdır. Ancak işletmenin bir dönemde öğrenci alıp almadığı Enrollment üzerinden izlenir |

### İşletme ve Dönem İlişkisi

İşletmeler dönemden bağımsızdır — bir işletme birden fazla dönemde aktif olabilir veya bir dönemde hiç öğrenci almayabilir. Dönemsel "snapshot" bilgisi doğal olarak oluşur:

- **Enrollment:** Hangi dönemde hangi öğrenci hangi işletmeye yerleştirildi → `InternshipPlacement.AcademicPeriodId`
- **Contract:** Hangi dönemde hangi işletmeyle sözleşme yapıldı → `InternshipContract.AcademicPeriodId`
- **Business kapanma/pasif olma:** Business modülündeki durum geçişleri (event sourced) zaten tarihli — geçmiş döneme ait kaydı sorgulamak mümkün

Bu sayede "2024-2025 döneminde aktif olan işletmeler" gibi sorgular Enrollment/Contract verileri üzerinden yapılabilir; Business modülüne dönem alanı eklemeye gerek yoktur.

### Geçmiş Dönem Yazma Koruması (Backend Gate)

Tüm yazma endpoint'leri (command handler'lar) dönem durumunu kontrol eder:

```text
Yazma isteği geldi → AcademicPeriodId kontrol et
  → Dönem Active ise → işlemi yap
  → Dönem Closed ise → DomainException("Bu dönem kapatılmıştır, yazma işlemi yapılamaz")
```

**Uygulama yöntemi:**

- Her modülün command handler'ı dönem durumunu kendi `AcademicPeriodView` projection'ından okur
- `AcademicPeriodView`: Institution modülünün `AcademicPeriodCreated` / `AcademicPeriodClosed` event'lerini dinleyen cross-module read model (her modülün kendi schema'sında)
- Bu pattern mevcut modüler monolit kurallarıyla uyumludur: event dinle → kendi projection'ını güncelle → kendi handler'ında oku

### Frontend — Global Dönem Seçici

**Pinia store:** `useAcademicPeriodStore`

- `activePeriod` — Seçili aktif dönem bilgisi
- `periods` — Kurumun tüm dönemleri (aktif + kapalı)
- Uygulama açılışında otomatik yüklenir

**Sol menü (MainLayout):** Drawer üst kısmında dönem seçici (q-select)

- Aktif dönem varsayılan seçili gelir
- Kapalı dönemler seçilebilir (salt okunur modda gezinme için)
- Kapalı dönem seçildiğinde tüm yazma butonları devre dışı kalır

**API istekleri:** Tüm liste/sorgu endpoint'lerine `academicPeriodId` query parametresi eklenir

- Frontend her istekte global store'daki aktif dönem ID'sini gönderir
- Backend dönem filtresi olmayan sorguya izin vermez (zorunlu parametre)

### Event Flow

```text
Institution → AcademicPeriodCreated event
  → Enrollment dinler → kendi AcademicPeriodView projection'ını oluşturur
  → Contract dinler → kendi AcademicPeriodView projection'ını oluşturur
  → Attendance dinler → kendi AcademicPeriodView projection'ını oluşturur
  → Payment dinler → kendi AcademicPeriodView projection'ını oluşturur
  → Coordination dinler → kendi AcademicPeriodView projection'ını oluşturur
  → Reporting dinler → denormalize view'lara dönem bilgisi eklenir

Institution → AcademicPeriodClosed event
  → Tüm modüller dinler → AcademicPeriodView.Status = Closed
  → Bu dönemle ilgili yazma işlemleri artık reddedilir
```

### Yeni Dönem Başlangıç Süreci

1. Kurum yöneticisi yeni dönem oluşturur (`POST /api/institutions/{id}/academic-periods`)
2. Önceki dönem otomatik kapatılır → `AcademicPeriodClosed` event
3. Yeni dönem aktif olur → `AcademicPeriodCreated` event
4. Enrollment modülünde yeni dönem için boş öğrenci listesi hazır
5. Önceki dönemden devam eden öğrenciler varsa → transfer/taşıma işlemi (opsiyonel)
6. Yeni yerleştirmeler, sözleşmeler yeni dönem altında oluşturulur
