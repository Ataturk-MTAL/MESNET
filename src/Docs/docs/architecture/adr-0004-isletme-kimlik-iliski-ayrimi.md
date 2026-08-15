---
title: 'ADR-0004: İşletme kimlik/ilişki ayrımı'
---

# ADR-0004: İşletme durumu iki katmandır — kimlik ve ilişki

| | |
|---|---|
| **Durum** | **ÖNERİ — karar bekliyor** |
| **Karar sahibi** | Proje sahibi |
| **İlgili** | #151 madde 4-6 · [ADR-0003](./adr-0003-cok-kiracilik.md) · #119, #147 |
| **Tetikleyici** | İkinci okul. Tek okulluyken hiçbir belirti üretmez. |

## Karar (önerilen)

İşletmeye ait durum bilgisi **iki ayrı katmana** bölünür:

| Katman | Soru | Kapsam | Kiracı damgası |
|---|---|---|---|
| **Kimlik** | İşletme fiilen var mı, faaliyette mi? | Tüm okullar | **Yok** (paylaşımlı) |
| **İlişki** | Bu okul bu işletmeyle çalışıyor mu? | Yalnız o okul | **Var** |

`Business` belgesi kimlik katmanında kalır ve **paylaşımlı** olmayı sürdürür. Okula özel her
karar yeni bir `BusinessRelationship` belgesine taşınır ve o belge **kiracı damgalıdır**.

## Bağlam — ölçülen bugünkü durum

Bu ADR tahminle değil ölçümle yazıldı (`dev` @ 278e3e9).

### İşletme kataloğu paylaşımlıdır, kararlar da öyle

`DocumentTenancyMap` → `["Business"] = Shared`. Yani `Business` belgesindeki **her alan** tüm
okullar için ortaktır. Bugün o belgede duran ve gerçekte **okula özel olması gereken** alanlar:

| Alan | Bugünkü davranış | Olması gereken |
|---|---|---|
| `StatusName` (`PendingApproval`/`Active`/`Rejected`/`Inactive`) | Küresel | **İlişki** — her okul kendisi için onaylar/pasife alır |
| `AuthorizedBranches` / `ActiveBranchCodes` (#119) | Küresel | **İlişki** — belge incelemesini yapan okulun kararı |
| `Capacity` | Küresel | **İlişki** — kontenjan okul başına ayrılır |
| `Documents` | Küresel | Karma — belge kimliğe ait, **onayı** ilişkiye |
| `HasAssignedTeacher` | Küresel | **İlişki** — koordinatör atayan okul |
| `ClosureReports` (#151/1) | Küresel, **kurum kırılımlı** ✅ | Doğru: kimlik kararı, kurumdan sayılıyor |
| `Name`, `Address`, `TaxNumber`, `Location`, `PersonnelCount`, `IsPublicInstitution` | Küresel ✅ | Doğru: kimlik |

### En keskin kanıt: alan yetkisi hangi okulun?

```csharp
public sealed record AuthorizeBusinessForBranches(
    Guid BusinessId,
    List<BranchAuthorizationItem> Branches);      // ← kurum kimliği YOK
```

#119'un özeti *"idarenin belge incelemesi sonucu verdiği yetkiler"* diyor — **hangi idare?**
Bugün A okulunun belge incelemesi sonucu verdiği alan yetkisini B okulu **devralıyor** ve o
alandan öğrenci yerleştirebiliyor. `BusinessBranchAuthorizationView` de `Shared`.

### Askıya alma bütün okulları etkiliyor

`SuspendBusinessHandler` / `DeactivateBusinessHandler` küresel `Status`'ü yazıyor. A okulunun
kötü deneyimi B okulunun listesinden de düşürüyor.

### İyi haber: okuma yüzeyi dar

`BusinessStatus` tipi **yalnız Business modülünde** kullanılıyor. Diğer modüller durumu
denormalize bir `bool` üzerinden görüyor:

```csharp
// Enrollment.Core/ReadModels/BusinessProfileView.cs
public bool IsActive { get; set; } = true;      // BusinessActivated/Closed/Deactivated tüketicileri yazıyor
```

Yani okuma tarafında değişecek şey tip değil, **olayın hangi okula ait olduğu** bilgisi.

> ⚠ `BusinessProfileView` bugün `Shared`. İlişki katmanı gelince bu görünüm **kiracı damgalı**
> olmalı, yoksa "A okulu pasife aldı" bilgisi B'nin görünümüne yazılmaya devam eder.

## Gerekçe

### Neden alan taşımak yetmez

`Business.Status`'e `InstitutionId` eklemek işe yaramaz: bir işletmeyle **N okul** çalışır, yani
durum tekil bir alan değil **koleksiyondur**. Koleksiyonu paylaşımlı belgenin içine gömmek de
kiracı izolasyonunu bozar — A okulunun satırını B okuma yetkisiyle görür. ADR-0003'ün satır
bazlı izolasyonu ancak **ayrı belge** ile çalışır.

### Neden `Business` paylaşımlı kalmalı

Kimlik katmanı bilerek paylaşımlıdır: iki okulun aynı firmayı ayrı ayrı kaydetmesini engelleyen
şey vergi kimliği tekilliğidir (#150). `Business` kiracı damgalı yapılırsa o tekillik okul içine
düşer ve kopya katalog geri gelir.

### Neden şimdi karar, sonra kod

Tek okulluyken **hiçbir belirti yok**: A okulunun askısı B'yi etkiliyor ama B yok. İkinci okul
açıldığı gün altı madde birden görünür hâle gelir ve o an veri göçü çok daha pahalıdır — ADR-0003
"geçiş tek okulluyken yapılır" kuralını aynı gerekçeyle koymuştu.

## Önerilen model

```
Business                          (Shared — kimlik)
  Id, Name, Address, TaxNumber, Location, PersonnelCount,
  IsPublicInstitution, Sectors, ClosureReports[], StatusName ∈ {Active, Closed, Rejected}

BusinessRelationship              (Tenant — ilişki, okul başına bir satır)
  Id, BusinessId, InstitutionId,
  StatusName ∈ {PendingApproval, Active, Inactive, Rejected},
  AuthorizedBranches[], Capacity, HasAssignedTeacher, ApprovedAt
```

**Doğal anahtar:** `(BusinessId, InstitutionId)` — kiracı damgası okulu zaten ayırdığı için
pratikte `BusinessId` yeterli; kısmi unique index kısa isimle (#237 deseni).

### Durum sözlüğü daralır

`BusinessStatus` kimlik katmanında yalnız üç değer taşır: `Active`, `Closed`, `Rejected`.
`PendingApproval` ve `Inactive` ilişki katmanına iner — çünkü ikisi de "bu okul için" anlamındadır.

### Olaylar okul taşır

Bugün `BusinessApproved`, `BusinessDeactivated`, `BusinessActivated` kurum bilgisi taşımıyor.
İlişki katmanı gelince **taşımak zorundalar** — sona eklenir ve varsayılanlı olur (#230'daki
`KeycloakUserId`, #242'deki `AcademicPeriodId` deseni), yoksa saklı olaylar okunamaz hâle gelir.

## Sonuçları

- Her okul aynı işletmeyi **kendisi için** onaylar; A'nın reddi B'yi bağlamaz
- Alan yetkisi veren okul kendi yetkisini verir (#119'un asıl niyeti)
- Kontenjan okul başına ayrılır — bugün tek havuz
- Kapatma (kimlik) yeter sayıyla verilmeye devam eder (#151/1, **değişmiyor**)
- Kapatma/açma asimetrisi korunur (#151/2, **değişmiyor**)

### Bu ADR'ın açtığı iki iş

- **#151 madde 5** — çapraz kurum bildirimi: "bu işletmeyle çalışan diğer okullar" sorgusu
  ilişki kayıtlarından yanıtlanabilir hâle gelir. `NotificationTarget` bugün tekil
  `InstitutionId` alıyor; kiracı listesi boyutu eklenmeli
- **#151 madde 6** — süzgeç davranışı: "bir okulda öğrencisi yok ama başka okulda var" sorusu
  ancak ilişki katmanı varken anlam kazanır

## Riskler

| Risk | Azaltım |
|---|---|
| Veri göçü: mevcut `Business` alanları ilişkiye kopyalanmalı | Tek okul varken bire bir kopya; `RegisteredByInstitutionId` hedefi verir |
| Saklı olaylar kurum taşımıyor | Alan **sona** eklenir, varsayılanlı; eski olaylar `Guid.Empty` okur |
| `BusinessProfileView` bugün `Shared` | Damgalanması tablo yeniden inşası ister — ADR-0003 uyarısı geçerli |
| Frontend işletme listesi tek duruma bakıyor | İlişki durumu ayrı alan olarak dönmeli; liste/filtre gözden geçirilir |

## Kilitlenecek kurallar (karar verilirse)

- `Business` belgesinde okula özel alan **bulunamaz** — kaynak taraması testi (`BusinessProvenanceDriftTests` deseni)
- `BusinessRelationship` `DocumentTenancyMap`'te `Tenant` sınıflandırılmalı (`DocumentTenancyDriftTests` zaten sınıflandırılmamış belge bırakmıyor)
- Alan yetkisi veren komut kurum kimliği **istekten almaz**, kiracıdan türetir (#235'in `KnownDebt` listesine satır eklenmemeli)

## Karar verilmesi gerekenler

1. **Onay ilişki düzeyine inince** yeni okul bir işletmeyle çalışmaya başlamak için baştan onay
   sürecinden mi geçecek, yoksa "başka okul onaylamış" bilgisi bir kolaylık mı sağlayacak?
2. **Belgeler** (`Documents`) kimlikte mi kalacak, onayları mı ilişkiye taşınacak? Ustalık
   belgesi işletmenin niteliğidir (kimlik) ama onaylayan okuldur (ilişki).
3. **Kontenjan** okul başına mı ayrılacak, yoksa tek havuzdan mı paylaşılacak? İkisi de savunulabilir:
   ayrı kontenjan yönetimi basitleştirir, tek havuz gerçeğe daha yakındır (işletmenin fiilî kapasitesi tektir).
