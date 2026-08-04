---
title: Dağıtım Ön Koşulları
sidebar_label: Dağıtım Ön Koşulları
---

# Dağıtım Ön Koşulları

Bazı değişiklikler dağıtımdan sonra **elle bir adım** gerektirir. Bu adımlar atlanınca sistem
**hata vermez** — özellik sessizce çalışmaz. Belirti hep aynıdır: liste boş gelir, sayı sıfır
çıkar, buton hiçbir şey yapmaz.

Bu sayfa o adımların tamamını tek yerde tutar.

## Ne zaman gerekir?

| Değişiklik | Gereken adım |
| --- | --- |
| Yeni **read-model** (denormalize görünüm) eklendi | İlgili resync ucu çağrılır |
| Var olan read-model'e **yeni alan** eklendi | İlgili resync ucu çağrılır |
| Realm'e yeni **rol / politika / client** eklendi | Realm doğrulaması (aşağıya bakınız) |

Ortak kök neden: olay-beslemeli read-model'ler yalnız **bundan sonra** gelen olayları yazar.
Seeder idempotent olduğu için kaynağı yeniden yaratmaz, yani olay bir daha yayınlanmaz ve mevcut
kayıtlar için görünüm hiç dolmaz.

## Realm ayarları — artık otomatik yakalanıyor

Keycloak realm import **tek seferliktir**: `mesnet-realm.json`'a sonradan eklenen rol, politika
ya da client ayarı **mevcut bir kaba hiç ulaşmaz**.

Bu artık elle takip edilmez. Açılışta `RealmVerificationHostedService` çalışan realm'i
`RealmInvariants` ile karşılaştırır:

- **Development:** sapma varsa **açılış durur**, düzeltme yolu hata mesajındadır
- **Diğer ortamlar:** `LogCritical`
- Keycloak'a ulaşılamaması sapma sayılmaz — kontrol atlanır, açılış durmaz

Gerçek bir örnekte depoda 11 rol tanımlıyken çalışan realm'de yalnız 6'sı vardı; eksik beşi
farklı sürümlerde eklenip her seferinde unutulmuştu.

## Resync / backfill uçları

Hepsi **idempotent**tir (tüketiciler `session.Store` ile upsert yapar), birden çok kez
çağrılabilir.

| Uç | Ne yapar |
| --- | --- |
| `POST /api/students/resync-projections` | Tüm öğrenciler için `StudentRegistered` yeniden yayınlanır — Attendance/Contract `StudentNameView`, Reporting ve Payment görünümlerini tazeler |
| `POST /api/placements/resync-projections` | Tüm **aktif** yerleştirmeler için `StudentPlaced` yeniden yayınlanır — Payment `PlacementView`, Coordination not giriş görünümleri |
| `POST /api/placements/backfill-branch-authorizations` | İşletmelerin alan yetkilerini mevcut yerleştirmelerden doldurur |
| `POST /api/businesses/resync-projections` | Tüm işletmeler için `BusinessUpdated` yeniden yayınlanır — diğer modüllerin işletme görünümleri |
| `POST /api/coordination/teachers/resync-views` | Koordinasyon görünümlerini kurum bazında yeniden kurar |
| `POST /api/coordination/weekly-visits/resync` | Haftalık ziyaret olaylarını yeniden yayınlar |
| `POST /api/institutions/staff/resync-branch-codes` | #126 öncesi kullanıcıların alan kapsamını personel kaydındaki bilgiden doldurur — **uydurmaz**, yalnız dolu olanı taşır |
| `POST /api/security/users/resync-display-names` | Kullanıcı görünen adlarını tazeler |

### Yerleştirme resync'i: atlanan kayıtlar

`POST /api/placements/resync-projections` yanıtı `{ placementCount, skipped }` döndürür.
**`skipped` sıfırdan büyükse bakın** — kaynak kaydı (öğrenci profili ya da işletme görünümü)
bulunamayan yerleştirmeler yayınlanmamıştır, çünkü eksik adla yayın tüketicilerin denormalize
alanlarını boş dizeyle ezerdi.

Tamamlanmış ve feshedilmiş yerleştirmeler bilerek atlanır: yeniden yayınlanırlarsa tüketici
modüllerde yeniden "aktif" işaretlenirlerdi.

**Okulda staj (işverensiz) yerleştirmeler atlanmaz** — işletmenin yokluğu orada eksik veri
değildir. Kural `PlacementResyncPolicy` içinde adlandırılmış ve testle kilitlenmiştir; koşulun
"işletme kaydı yoksa atla" diye sadeleştirilmesi okulda staj yapan her öğrenciyi sessizce
düşürür ve dönem notu girilemez hâle getirir.

## Sırayı bozmayın

Bir adım başka bir adımın verisini üretiyorsa sıra önemlidir. Örnek: koordinasyon zinciri
dağıtımında önce **yetki backfill'i**, sonra **görünüm resync'i** gerekir — ters sırada
yerleştirme tümden durur.
