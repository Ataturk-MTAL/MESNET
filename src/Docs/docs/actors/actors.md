---
title: Aktör Tanımları
---

# MESNET PROJESİ AKTÖRLERİ VE GÖREVLERİ

> **Kapsam Notu:** Blockchain aktörleri (Blockchain Sistem Yöneticisi, Doğrulayıcı) ve Tenant Yöneticisi Phase 2 kapsamındadır.
> Phase 1'de bu aktörler ve ilgili yetkiler implementasyona alınmayacaktır.

> **Kimlik ve Yetki Altyapısı (Security Modülü):** Aşağıdaki tüm aktörlerin kimlik doğrulaması ve yetkilendirmesi **Security modülü** üzerinden yürür. Hesaplar Keycloak'ta (OAuth2/OIDC, PKCE) tutulur; her aktörün rolü 6 realm rolünden birine eşlenir (`InstitutionManager`, `InstitutionStaff`, `Teacher`, `Student`, `DepartmentHead`, `CompanyManager`). Aktörler sisteme **davet akışı** ile eklenir: yetkili kullanıcı davet oluşturur → Kurum Müdürü/Müdür Yardımcısı onaylar → davet edilen kişi hesabını tamamlar (`UserCreated` event'i ile ilgili profil — öğretmen, öğrenci, işletme yetkilisi — otomatik oluşur). Rol bazlı yetkiler ve doğrudan izinler için bkz. [Claims ve Permissions](#claims-ve-permissions) bölümü.

## ~~Üst Yönetim (Tenant) Aktörleri~~ (Phase 2)

- **Tenant Yöneticisi** *(Phase 2 — Tenant modülü aktifleştirildiğinde)*
  - Kurum ekleme/düzenleme
  - Kurum müdürü hesapları yönetimi
  - Sistem geneli raporlama ve istatistik
  - Asgari ücret ve diğer sistem parametreleri yönetimi

## Kurum Aktörleri

- **Kurum Müdürü**
  - Personel yetkilendirme
  - Protokol onayları
  - Öğrenci-işletme eşleştirme onayı
  - Özel sektör protokolleri oluşturma

- **Müdür Yardımcısı**
  - Staj işlemleri koordinasyonu
  - Evrak takibi ve onayı
  - Öğretmen görevlendirmeleri
  - Dekont ve maaş süreçleri yönetimi

- **Program Koordinatörü**
  - Eğitim programları yönetimi
  - Sertifikasyon süreçleri
  - Eğitmen atamaları
  - Program değerlendirme ve raporlama

- **Kurum Yetkilendirdiği Personel**
  - Öğrenci kayıt işlemleri
  - Belge doğrulama
  - Devamsızlık takibi
  - Maaş hesaplamaları

## İşletme Aktörleri

- **İşletme Yöneticisi**
  - Öğrenci talep etme
  - Belge yükleme ve güncelleme
  - Devamsızlık bildirimi
  - Maaş dekontları yönetimi

- **Usta Öğretici**
  - Öğrenci eğitimi
  - Performans değerlendirmesi
  - Devam takibi
  - Beceri eğitimi

## Eğitim Personeli Aktörleri

- **Alan Şefi (Bölüm Başkanı)**
  - Alan bazlı işletme dağıtımı yapma
  - Zümre karar tutanağı oluşturma
  - Öğretmen iş yükü hesaplama ve kontrolü
  - Koordinatör öğretmen-işletme ataması

- **Öğretmen (Koordinatör)**
  - İşletme ziyaretleri
  - Öğrenci takibi
  - Rapor oluşturma
  - Dekont onayı

- **Eğitmen**
  - Özel sektör eğitimleri
  - Değerlendirme yapma
  - Program içeriği hazırlama
  - Sertifika önerisi

## Öğrenci Aktörleri

- **Stajyer Öğrenci**
  - Staj başvurusu
  - Devam durumu görüntüleme
  - Maaş onayı
  - İşletme değerlendirme
  - Sertifika takibi

## ~~Blockchain Aktörleri~~ (Phase 2)

- **Blockchain Sistem Yöneticisi** *(Phase 2)*
  - Smart contract yönetimi
  - NFT basım işlemleri
  - Blockchain altyapı yönetimi
  - Gas optimizasyonu

- **Doğrulayıcı (İşveren/Kurum)** *(Phase 2)*
  - Sertifika doğrulama
  - NFT görüntüleme
  - Yeterlilik kontrolü

## Yetki Seviyeleri ve Erişim Hakları

### Üst Seviye Yetkiler

- **Sistem Yönetimi**
  - Kurum Müdürü
  - ~~Tenant Yöneticisi~~ *(Phase 2)*

### Orta Seviye Yetkiler

- **Operasyonel Yönetim**
  - Müdür Yardımcısı
  - Program Koordinatörü
  - İşletme Yöneticisi
  - Öğretmen

### Temel Seviye Yetkiler

- **Süreç Katılımcıları**
  - Stajyer Öğrenci
  - Usta Öğretici
  - Eğitmen
  - Doğrulayıcı

## Modül Bazlı Yetki Matrisi

| Modül                              | Üst Seviye | Orta Seviye | Temel Seviye |
|------------------------------------|------------|-------------|--------------|
| ~~Tenant Yönetimi~~ (Phase 2)      | Tam Yetki  | Görüntüleme | Yok          |
| Kurum Yönetimi                     | Tam Yetki  | Kısıtlı     | Görüntüleme  |
| İşletme Yönetimi                   | Tam Yetki  | Tam Yetki   | Kısıtlı      |
| Stajyer İşlemleri                  | Tam Yetki  | Tam Yetki   | Kısıtlı      |
| ~~Blockchain~~ (Phase 2)           | Tam Yetki  | Görüntüleme | Görüntüleme  |
| Protokol Yönetimi                  | Tam Yetki  | Kısıtlı     | Yok          |

## Detaylı İzin Matrisi

### Modül Bazlı İzinler

| Modül/Fonksiyon                    | Tenant Admin | Kurum Müdürü | Müdür Yard. | Koordinatör | Öğretmen | İşletme Yön. | Öğrenci     |
|------------------------------------|--------------|--------------|-------------|-------------|----------|--------------|-------------|
| ~~Tenant Yönetimi~~ (Phase 2)      | Tam          | Yok          | Yok         | Yok         | Yok      | Yok          | Yok         |
| Kurum Yönetimi                     | Tam          | Kısıtlı      | Görüntüleme | Yok         | Yok      | Yok          | Yok         |
| Öğrenci İşlemleri                  | Tam          | Tam          | Tam         | Kısıtlı     | Kısıtlı  | Görüntüleme  | Kendi       |
| İşletme İşlemleri                  | Tam          | Tam          | Tam         | Kısıtlı     | Kısıtlı  | Kendi        | Yok         |
| Protokol Yönetimi                  | Tam          | Tam          | Kısıtlı     | Görüntüleme | Yok      | Yok          | Yok         |
| ~~Blockchain İşlemleri~~ (Phase 2) | Tam          | Görüntüleme  | Yok         | Yok         | Yok      | Yok          | Kendi       |
| ~~Sertifika Yönetimi~~ (Phase 2)   | Tam          | Tam          | Kısıtlı     | Kısıtlı     | Yok      | Yok          | Görüntüleme |
| Devamsızlık Takibi                 | Tam          | Tam          | Tam         | Tam         | Tam      | Giriş        | Görüntüleme |
| Maaş İşlemleri                     | Tam          | Tam          | Tam         | Onay        | Onay     | Dekont       | Onay        |

### İşlem Bazlı İzinler

#### Tenant Yönetici İzinleri

- Tüm sistem yapılandırması
- Kurum ekleme/silme/düzenleme
- Blockchain smart contract yönetimi
- Sistem geneli raporlar
- Asgari ücret tanımlama

#### Kurum Müdürü İzinleri

- Personel yetkilendirme
- Protokol onayı
- Öğrenci-işletme eşleştirme
- Maaş onayı
- Belge şablonları yönetimi

#### Müdür Yardımcısı İzinleri

- Öğrenci işlemleri
- Evrak takibi
- Öğretmen görevlendirme
- Maaş hesaplama
- Devamsızlık yönetimi

#### Program Koordinatörü İzinleri

- Program içeriği oluşturma
- Eğitmen atama
- Sertifika hazırlama
- Değerlendirme yapma
- Rapor oluşturma

#### Öğretmen İzinleri

- Öğrenci takibi
- İşletme ziyareti
- Devamsızlık girişi
- Dekont onayı
- Rapor oluşturma

#### İşletme Yöneticisi İzinleri

- Öğrenci talebi
- Devamsızlık bildirimi
- Belge yükleme
- Dekont yükleme
- Usta öğretici atama

#### Öğrenci İzinleri

- Staj başvurusu
- Devamsızlık görüntüleme
- Maaş onayı
- Sertifika görüntüleme
- İşletme değerlendirme

### Özel Durum İzinleri

#### ~~Blockchain İşlemleri~~ (Phase 2)

- **Tam Yetki:** Tenant Admin, Blockchain Yöneticisi
- **Sınırlı Yetki:** Kurum Müdürü (Görüntüleme)
- **Kendi:** Öğrenci (Cüzdan işlemleri)

#### ~~Sertifikasyon İşlemleri~~ (Phase 2)

- **Hazırlama:** Program Koordinatörü
- **Onay:** Kurum Müdürü
- **NFT Basım:** Blockchain Yöneticisi
- **Görüntüleme:** Öğrenci, Doğrulayıcı

#### Protokol İşlemleri

- **Oluşturma:** Kurum Müdürü
- **Onay:** Tenant Admin
- **Uygulama:** Program Koordinatörü
- **Takip:** Müdür Yardımcısı

## Claims ve Permissions

### Role-Based Claims

#### Tenant.Admin

- tenant.manage
- tenant.system.configure
- tenant.reports.view
- blockchain.admin
- protocols.approve
- institutions.manage

#### Institution.Manager

- institution.manage
- staff.authorize
- protocols.create
- student.approve
- certificates.approve
- payments.approve

#### Institution.Deputy

- student.manage
- documents.track
- teacher.assign
- salary.calculate
- attendance.manage

#### Program.Coordinator

- program.manage
- trainer.assign
- certificate.prepare
- evaluation.manage
- reports.create

#### Teacher

- student.track
- company.visit
- attendance.entry
- payment.approve
- report.create

#### Company.Manager

- company.manage
- student.request
- document.upload
- attendance.report
- payment.upload
- trainer.assign

#### Student

- internship.apply
- attendance.view
- salary.confirm
- certificate.view
- company.evaluate

#### Blockchain.Admin (Phase 2)

- blockchain.manage
- smartcontract.deploy
- nft.mint
- gas.optimize
- blockchain.monitor

### Resource-Based Permissions

#### Tenant Management

- tenant:read
- tenant:write
- tenant:delete
- tenant:configure

#### Institution Management

- institution:read
- institution:write
- institution:delete
- institution:configure

#### Student Operations

- student:read
- student:write
- student:delete
- student:approve

#### Company Operations

- company:read
- company:write
- company:delete
- company:approve

#### Protocol Management

- protocol:read
- protocol:write
- protocol:delete
- protocol:approve

#### Blockchain Operations (Phase 2)

- blockchain:read
- blockchain:write
- blockchain:deploy
- blockchain:mint

#### Certificate Management (Phase 2)

- certificate:read
- certificate:write
- certificate:approve
- certificate:mint

#### Attendance Tracking

- attendance:read
- attendance:write
- attendance:approve
- attendance:report

#### Payment Operations

- payment:read
- payment:write
- payment:approve
- payment:process

### Composite Claims

#### Program Management

```json
{
  "role": "program_coordinator",
  "permissions": [
    "program:manage",
    "trainer:assign",
    "certificate:prepare",
    "evaluation:manage"
  ]
}
```

#### Student Management

```json
{
  "role": "institution_deputy",
  "permissions": [
    "student:manage",
    "attendance:manage",
    "payment:calculate",
    "document:track"
  ]
}
```

#### Company Management

```json
{
  "role": "company_manager",
  "permissions": [
    "company:manage",
    "student:request",
    "document:upload",
    "payment:upload"
  ]
}
```

#### Certificate Workflow (Phase 2)

```json
{
  "workflow": "certificate_management",
  "steps": [
    {
      "role": "program_coordinator",
      "permission": "certificate:prepare"
    },
    {
      "role": "institution_manager",
      "permission": "certificate:approve"
    },
    {
      "role": "blockchain_admin",
      "permission": "certificate:mint"
    }
  ]
}
```

### Keycloak Client Roles

```json
{
  "roles": {
    "tenant_admin": {
      "name": "tenant_admin",
      "composite": false,
      "clientRole": true,
      "permissions": [
        "tenant:*",
        "institution:*"
      ]
    },
    "institution_manager": {
      "name": "institution_manager",
      "composite": false,
      "clientRole": true,
      "permissions": [
        "institution:manage",
        "staff:authorize",
        "protocol:create"
      ]
    },
    "program_coordinator": {
      "name": "program_coordinator",
      "composite": false,
      "clientRole": true,
      "permissions": [
        "program:manage",
        "certificate:prepare",
        "evaluation:manage"
      ]
    },
    "_comment_blockchain_admin": "Phase 2 - blockchain_admin rolü Phase 1'de aktif değildir"
  }
}
```

### Policy Configurations

```json
{
  "policies": {
    "_comment": "Phase 2 - certificate_approval ve blockchain_operations policy'leri Phase 1'de aktif değildir"
  }
}
