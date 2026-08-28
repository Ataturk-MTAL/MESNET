# Denetim izi (C parçası)

**Tarih:** 28.08.2026
**Durum:** Tasarım onaylandı, uygulama planı yazılmadı
**Kapsam:** Yalnız C — denetim izi. Tam yazma yetkisi ve aktif bağlam değiştirme (B) bu spec'in dışındadır.

---

## Problem

Depoda denetim altyapısı **hiç yok** — `audit` geçen tek üretim dosyası bile yok. Sağlayıcı bilgi kısmen var ama dağınık: **124 yerde** domain olaylarına gömülü "kim yaptı" alanı (`ApprovedById`, `UpdatedById`, `PerformedBy`). Bunlar tek tek anlamlı, ama birleşik bir iz oluşturmuyor ve gezilebilir bir yüzeyleri yok. "Geçen hafta bu öğrencinin devamsızlığını kim değiştirdi" sorusunun bugün cevabı yok.

Bu, B parçasının **ön koşulu**: il/ilçe yetkilisine tam yazma yetkisi, izi olmadan verilirse bir kişi bütün okulların kiracı sınırını taşır ve hiçbir kayıt kalmaz. Sıra bağlayıcıdır — C, B'den önce.

---

## Karar

**Her yazma komutu, tek bir genel Wolverine middleware'inde kaydedilir.** Modüllere dokunulmaz; yeni bir komut kendiliğinden kapsanır.

### Yakalama noktası ve süzgeç

`IPolicies.AddMiddleware<T>(Func<HandlerChain, bool>)` (Wolverine 6.14 — imza doğrulandı) genel middleware'i süzgeçle kaydetmeye izin verir.

Süzgeç **ad alanı konvansiyonudur**: mesaj tipi `.Commands` ad alanındaysa kaydedilir.

- Konvansiyon depoda zaten klasör yapısıyla zorlanıyor (`Commands/` ve `Queries/` ayrı) ve `InstitutionScopeDriftTests` de ona dayanıyor — yani yeni bir kural icat edilmiyor, var olan kural kullanılıyor.
- `Queries/` kaydedilmez: okuma iz üretmez, aksi hâlde hacim listeleme trafiğiyle dolar.
- `Consumers/` kaydedilmez: onlar kullanıcı eylemi değil, olay tepkisidir. Kullanıcı eylemi zaten onu tetikleyen komutta kaydedilmiştir.

### Kayıt biçimi — gövde YOK

```
AuditEntry
  Id                      Guid
  OccurredAt              DateTimeOffset (UTC)
  ActorId                 Guid
  ActorName               string        (denormalize)
  CommandType             string        (kısa ad, ör. "MarkAttendance")
  Module                  string        (ör. "Attendance")
  TenantId                string?
  ActorInstitutionId      Guid?
  SubjectInstitutionId    Guid?
  SubjectInstitutionPath  string?
  CrossedTenantBoundary   bool
  Outcome                 AuditOutcome  (SmartEnum)
  ErrorCode               string?
  TargetIds               Dictionary<string, Guid>
  DurationMs              int
```

**Komut gövdesi saklanMAZ.** Gövdeler sağlık raporu, maaş ve öğrenci verisi taşıyor; ize kopyalamak kiracı damgalı belgelerin dışında **ikinci bir hassas veri kopyası** yaratırdı ve silme talebi geldiğinde iki yerden silmek gerekirdi. "Ne değişti" sorusu olay deposundan (`mt_events`) cevaplanır.

**`ActorName` bilinçli olarak denormalizedir.** Kullanıcı kaydı silinse bile iz okunur kalmalıdır; ayrıca okuma anında ad çözmek modüller arası sorgu demektir ve yasaktır.

**`ErrorCode` saklanır, hata MESAJI saklanmaz.** `Error.Code` makine okunurdur ve sabittir; mesaj PII taşıyabilir (ör. ilçe adı, öğrenci adı).

**`CrossedTenantBoundary`** aktörün kurumu ile konu kurumu ayrıştığında `true`. B'nin sorumluluk sorgusu bu tek alana iner; hesaplanmış olarak saklanır çünkü sonradan türetmek iki alanın o günkü değerini bilmeyi gerektirir.

### Sonuç sınıflandırması

| Durum | `Outcome` | `ErrorCode` |
|---|---|---|
| Handler başarıyla döndü | `Succeeded` | — |
| `DomainException` (iş kuralı / kapsam reddi, HTTP 422) | `Rejected` | `Error.Code` |
| Diğer istisna | `Failed` | istisna tipinin adı |

Yetki reddi (403) middleware'e **ulaşmaz** — ASP.NET yetkilendirme katmanı isteği handler'dan önce keser. Bu bilinen bir boşluktur; bkz. Bilinen bedeller.

---

## En kritik karar: ayrı oturum

**Denetim yazması, komutun işleminden AYRI bir oturumda yapılır.**

Reddedilen bir komut `DomainException` atar ve Wolverine'in `AutoApplyTransactions()` politikası işlemi geri alır. Denetim satırı aynı oturumda yazılsaydı **ret kaydı da geri alınırdı** — yani en çok istediğimiz satır ("kim neye erişmeye çalıştı") tam da kaydedilmediği an olurdu.

Middleware `IDocumentStore` üzerinden kendi kısa ömürlü oturumunu açar ve bağımsız commit eder.

**Bedeli: iz en-iyi-çabadır.** Denetim yazması patlarsa iş akışı durmaz; hata loglanır ve devam edilir. Aksi hâlde bozuk bir denetim tablosu bütün okulu kilitlerdi. Garantili iz bloklayıcı bir tasarım ister; bir okul sisteminde erişilebilirliğin kazanması gerektiği kanısıyla bu seçildi ve **bilinçlidir**.

---

## Hedef kimliklerinin çıkarımı

Komutlar heterojendir; middleware onları tanımaz. `TargetIds`, **bilinen bir ad kümesinden** konvansiyonla çıkarılır:

`StudentId`, `ContractId`, `BusinessId`, `InstitutionId`, `AcademicPeriodId`, `PaymentId`, `AttendanceId`, `UserAccountId`, `TeacherId`

Küme sabit ve testle kilitlidir. Kümede olmayan bir ad kullanan komut **hedefsiz** kaydolur — satır yine oluşur (kim, ne, ne zaman durur), yalnız hangi kayda dokunulduğu yazılmaz. Bu, sessiz bir eksikliktir ve drift testi kümenin gerçekle ilişkisini korur.

**Yansıma (reflection) maliyeti:** komut tipi başına özellik listesi bir kez çözülüp önbelleğe alınır; istek başına yansıma yapılmaz.

---

## Kiracılık ve şema

- `AuditEntry` → `DocumentTenancyMap` içinde **`DocumentTenancy.Tenant`**. Satır bir okulun verisi hakkındadır ve satır düzeyinde süzülmelidir.
- Şema: `audit`.
- Kurum üstü işlemler (ulusal parametre, `rebuild-hierarchy`) `platform` kiracısına düşer — kimsenin okul kapsamında görünmezler, yalnız platform aktörü görür.
- İndeksler (kısa adlarla, PostgreSQL 64 karakter sınırı): `idx_audit_occurred`, `idx_audit_actor`, `idx_audit_subject_path`.

---

## Okuma

**Yeni kapsam ekseni doğmaz.** Okuma süzgeci A parçasındaki `InstitutionScopePolicy` ile aynıdır:
`SubjectInstitutionPath.StartsWith(okuyucununYolu)`.

| Uç | Kapsam | İzin |
|---|---|---|
| `GET /api/audit?scope=mine` | `ActorId == aktör` | yok — kendi geçmişini görmek izin gerektirmez |
| `GET /api/audit?scope=institution` | yol öneki | `audit:view:institution` |

Sayfalı (`PagedQuery` → `PagedResult<AuditEntryDto>`), süzgeçler: `actorId`, `commandType`, `outcome`, `from`/`to`, `crossedTenantBoundary`.

### Neden yeni bir `audit:` öneki

`institution:` önekli bir izin `InstitutionManager`'ın `institution:*` wildcard'ı üzerinden **her okul müdürüne** geçerdi (ADR-0002 önek tuzağı). Okul müdürünün kendi okulunun izini görmesi istenen bir şeydir, ama kararın wildcard'ın yan etkisiyle değil **açıkça** verilmesi gerekir. Yeni ve çakışmasız bir önek bunu sağlar.

`audit:view:institution` başlangıçta `InstitutionManager` ve `DeputyDirector` rollerine verilir.

---

## Saklama — 24 ay

Günlük çalışan bir `BackgroundService` yaşı geçen satırları siler.

- Süre **yapılandırmadan** gelir (`Audit:RetentionMonths`), sabit kodlanmaz.
- Kiracı damgalı satırları silmek kiracı başına oturum ister → `ITenantDirectory.GetActiveTenantsAsync()` ile dolaşır. **`IDocumentSession` enjekte edilmez** (proje kuralı: arka plan işleri `IDocumentStore` alır).
- Kaç satır sildiğini kiracı başına loglar — sessiz silme kabul edilemez.
- `platform` kiracısı da temizlenir.

---

## Arayüz

**Yeni:** `pages/audit/AuditLogPage.vue`, rota `/audit`. Menüde "Kurum Yönetimi → Son İşlemler".

`AppTable` + `useServerPagination`. Kapsam seçici: "İşlemlerim" / "Kurumumdaki işlemler" (ikincisi yalnız `audit:view:institution` varsa görünür — `PermissionGuard`).

Sütunlar: tarih, kim, ne (Türkçe etiket), konu kurum, sonuç rozeti, hedef kayıt. Kiracı sınırını aşan satır rozetle işaretlenir.

**Komut tipinin Türkçe etiketi** sunucudan gelir (`CommandType` İngilizce anahtar, `CommandLabel` Türkçe) — arayüz kendi eşleme tablosunu tutmaz, tutsaydı yeni komutta sessizce ham tip adı görünürdü.

---

## Testler

**Saf birim testleri:**
- Hedef kimliği çıkarımı: bilinen adlar, bilinmeyen ad (hedefsiz satır), birden çok kimlik
- Sonuç eşlemesi: başarı, `DomainException` → `Rejected` + `Error.Code`, diğer istisna → `Failed`
- `CrossedTenantBoundary` hesabı: eşit kurumlar, farklı kurumlar, kurumsuz aktör

**Kilit test — bu tasarımın varlık nedeni:**
Reddedilen bir komut (işlem geri alınır) yine de iz satırı bırakır. Bu test ayrı oturum kararını kilitler; aynı oturuma dönülürse kırmızıya döner.

**Drift testleri:**
- `Commands/` altındaki her mesaj tipi süzgece takılır — takılmayan varsa iz sessizce eksik olur
- Bilinen hedef-kimlik ad kümesi, komutlarda fiilen kullanılan `Guid` özellik adlarıyla karşılaştırılır; kümede olmayan yaygın bir ad belirirse test uyarır

**Ön yüz:** liste sayfasının sunucu sözleşmesi (kapsam, süzgeç ve sayfalama parametrelerinin gerçekten gönderildiği).

---

## Bilinen bedeller

1. **İz en-iyi-çabadır.** Depolama hatasında satır kaybolur ve iş akışı devam eder. Bilinçli: bozuk denetim tablosu okulu kilitlememelidir.
2. **Yetki reddi (403) ize girmez.** ASP.NET yetkilendirme katmanı isteği handler'dan önce keser, middleware hiç çalışmaz. İzdeki `Rejected` satırları yalnız `DomainException` kaynaklıdır (kapsam ihlali dahil — o guard middleware'de çalışır ve yakalanır). HTTP katmanı reddini kaydetmek ayrı bir ara katman ister; bu spec'in dışındadır.
3. **Hedef kimliği çıkarımı konvansiyona dayalıdır.** Yeni bir ad kullanan komut hedefsiz kaydolur.
4. **Gövde yoktur.** "Ne değişti" sorusu olay deposundan cevaplanır; belge tabanlı (event-sourced olmayan) varlıklarda böyle bir geçmiş yoktur — orada iz "dokunuldu" der, "şu değerden şuna" demez.
5. **Hacim bağı yalnız saklama süresidir.** Tüm yazma komutları kaydedilir; devamsızlık girişi bu alanda en yüksek hacimli komuttur.
6. **`Consumers/` kaydedilmez.** Bir olayın tetiklediği zincirin adımları görünmez; yalnız zinciri başlatan kullanıcı eylemi görünür.

---

## Sonraki parça

**B — aktif bağlam değiştirme + tam yazma yetkisi.** Üst barda kurum seçici (Dönem/Yarıyıl'ın üstünde). Gizli maliyeti akademik dönem: kurum değişince dönem listesi yeniden yüklenmezse A okulunun dönem kimliğiyle B okuluna yazma olur.

C bittiğinde B'nin ön koşulu karşılanmış olur: tam yetki artık izli verilebilir.
