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

## Modül Yapısı

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

**İş Kuralları:** Detaylar `src/Arch/BusinessRules.md` dosyasında

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

**İş Kuralları:** Devamsızlık gün kısıtlamaları ve limit kuralları `src/Arch/BusinessRules.md` dosyasında

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

**İş Kuralları:** Maaş hesaplama formülleri ve devlet katkısı kuralları `src/Arch/BusinessRules.md` dosyasında

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

- `TeacherSchedule` — Öğretmen haftalık ders programı (boş saatler)
- `WorkloadConfig` — Ek ders sınırları, mesafe kuralları, parametre

**Sorumluluklar:**
- Öğretmen-işletme atama
- Haftalık ziyaret programı oluşturma (ders programı formatı)
- Lokasyon bazlı rota optimizasyonu
- Karekodlu rapor oluşturma ve yazdırma
- Evrak teslim takibi (karekod veya manuel)
- Koordinatör-öğrenci iletişimi
- Öğretmen ders programı yönetimi (boş saat tespiti)
- Alan bazlı işletme dağıtımı (zümre karar tutanağı + müdür onayı)
- İş yükü hesaplama (ek ders sınırları, mesafe kontrolü)
- Dağıtım onay süreci (TASLAK → ZÜMRE_KARARI_ALINDI → MÜDÜR_ONAY_BEKLİYOR → ONAYLANDI / REDDEDİLDİ)

**İş Kuralları:** Ek ders ve görevlendirme kuralları `src/Arch/BusinessRules.md` Bölüm 11'de

**Storage tipi:** Hybrid — VisitSchedule, TeacherSchedule, WorkloadConfig document; VisitReport, DepartmentDistribution event sourced

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

**Dekont onay zinciri:**
```
Payment → ReceiptUploaded (işletme)
  → ReceiptApprovedByTeacher (öğretmen onayı)
  → ReceiptApprovedByInstitution (kurum onayı)
  → SalaryConfirmedByStudent (öğrenci onayı)
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

## PostgreSQL Schema Dağılımı

| Modül | Schema | Storage |
|-------|--------|---------|
| Business | `business` | Document + Event |
| Enrollment | `enrollment` | Document + Event |
| Contract | `contract` | Event Sourcing |
| Attendance | `attendance` | Event Sourcing |
| Payment | `payment` | Event Sourcing |
| Coordination | `coordination` | Document + Event |
| Institution | `institution` | Document |
| Tenant *(Phase 2)* | `tenant` | Document |
| Internship | `internship` | Saga state + Projection |
| Reporting | `reporting` | Document (denormalize) |
| Wolverine (paylaşımlı) | `wolverine` | Messaging infra |

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
  └── Endpoints/         # Wolverine HTTP endpoint'ler ([WolverinePost], [WolverineGet])

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
