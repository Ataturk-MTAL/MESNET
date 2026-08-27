# Kurum hiyerarşisi ve il/ilçe kapsam katmanı (A parçası)

**Tarih:** 27.08.2026
**Durum:** Tasarım onaylandı, uygulama planı yazılmadı
**Kapsam:** Yalnız A parçası — görünürlük. Yazma yetkisi (B) ve denetim izi (C) bu spec'in dışındadır.

---

## Problem

Bugün bir kullanıcının kurum kapsamı iki uçludur: `platform:tenant:manage` varsa **bütün**
okullar, yoksa **kendi** okulu. Arası yoktur. İl ya da ilçe millî eğitim yetkilisi diye bir
aktör yoktur; böyle bir kullanıcı bugün sisteme konsa `institution_id` claim'i boş kalır ve
`InstitutionScopePolicy.VisibleInstitutionFilter` ona `Guid.Empty` döndürür — yani **boş
liste** görür. Bu kasıtlıydı ("her şeyi görmek yerine hiçbir şey"), ama il yetkilisinin
kendi ilinin okullarını görmesi gereken bir dünyayı karşılamıyor.

İkinci eksik: il/ilçe müdürlüğünün sistemde **kaydı yoktur**. Gerçekte adı, adresi,
personeli ve müdürü olan bir kurumdur.

---

## Karar

**Kurumlar ağaç olur.** `Institution` belgesi düğüm hâline gelir; il müdürlüğü, ilçe
müdürlüğü ve okul aynı belge tipinin farklı **tipleridir**. Kullanıcı–kurum bağı tek kural
olarak kalır: herkes bir kuruma bağlanır, o kurumun tipi ne olursa olsun.

### Neden ağaç, neden iki sabit alan değil

İlk tasarım kapsamı `ProvinceCode` + `DistrictName` alan çiftiyle kurmayı öneriyordu.
Reddedildi, çünkü:

- İl yetkilisinin **kaydı olmuyordu**. `UserAccount.InstitutionId` boş kalıyor, kapsamı ayrı
  claim'lerden geliyordu — yani "kullanıcı bir kuruma bağlanır" kuralına ikinci, paralel bir
  mekanizma ekleniyordu.
- Kapsam ekseni **üçe** çıkıyordu (`institution_id` + `province_code` + `district_name`), üçü
  de ayrı ayrı üretilecek, ayrı ayrı doğrulanacak, ayrı otorite testleri isteyecekti.
- Ağaçta eksen **tek**: `institution_id`. Kapsam "hedefin yolu benim yolumla başlıyor mu"
  sorusuna iner.
- İlçe adı tek başına benzersiz değildir ("Merkez" 81 ilde var); alan çifti modelinde kapsam
  daima `(il, ilçe)` ikilisi taşımak zorundaydı. Ağaçta bu sorun kendiliğinden yoktur.

Karşı argüman — 30.07.2026 kapsam kararı ("Bakanlık düzeyi aktör, ulusal hiyerarşi ve iller
arası federasyon tasarlanmaz") — geçerliliğini korur ve bu tasarım onu **ihlal etmez**:
modellenen seviye sayısı yine il ve ilçedir. Değişen, o seviyelerin dize alanı yerine kayıt
olarak tutulması. Sonsuz derinlik ağacın bedava yan ürünüdür, hedeflenen özellik değildir;
bugün üretilen düğüm tipleri üçle sınırlıdır.

### Özyineleme maliyeti yoktur

Materyalize yol (`Path`) kullanılır; alt ağaç sorgusu `Path.StartsWith(aktörünYolu)` olur ve
Marten bunu `LIKE 'önek%'` çevirir. Ham SQL, `WITH RECURSIVE` ve her istekte ağaç yürüyüşü
**gerekmez**. Depo zaten Marten'in aynı sınıf çevirisine dayanıyor
(`QueryableExtensions.ApplySearch` → `string.Contains` → `ILIKE`).

---

## Veri modeli

`Institution` belgesine üç alan eklenir:

| Alan | Tip | Anlam |
|---|---|---|
| `ParentId` | `Guid?` | Üst düğüm. Kök (il) için `null`. |
| `NodeType` | `InstitutionNodeType` (SmartEnum) | `Province` / `District` / `School` |
| `Path` | `string` | Kökten kendisine kimlik zinciri, **daima `/` ile başlar ve `/` ile biter**: `/{ilId}/{ilçeId}/{okulId}/` |

**`Path` kimliklerden kurulur, adlardan değil.** İlçe adı düzeltilirse yol bozulmamalıdır.

**Sondaki `/` biçimin parçasıdır, süs değil.** Onsuz `/33/1` öneki `/33/10...` yolunu da yakalar ve bir ilçe yetkilisi kardeş ilçeyi görür. Ayraçla biten karşılaştırma bunu kapatır.

**Nullable, `required` DEĞİL.** Mevcut kayıtlar bu alanlar olmadan saklandı; `required`
yapılırsa System.Text.Json eksik alan yüzünden her eski kurumun okunmasını `JsonException`
ile keser (aynı tuzak `ProvinceCode` ve `BrandPaletteName` yorumlarında anlatıldı).

**Mevcut `ProvinceCode` / `DistrictName` alanları kalır** — okulun künyesidir ve geçişin
girdisidir. Kapsam kararı artık onlara bakmaz.

### İndeksler

`idx_institution_path`, `idx_institution_parent`. Kısa ad zorunlu: PostgreSQL tanımlayıcı
sınırı 64 karakter, Marten'in otomatik adı bunu aşar.

### Kiracılık değişmez

Kiracı **okuldur**. İl ve ilçe düğümleri kiracı değildir ve kiracı damgalı hiçbir veri
taşımaz; `DocumentTenancyMap` sınıflandırması olduğu gibi kalır (`Institution` →
`Identity`). Ağaç yalnız **kapsam** eksenidir; **izolasyon** ekseni okulda durur.

---

## Kapsam kararı

Tek yerde, saf fonksiyonda:

```
CanAccess(actorPath, targetPath, hasPlatformScope) =
    hasPlatformScope
    || (actorPath is not empty && targetPath.StartsWith(actorPath))
```

- Aktörün kendi düğümü de kapsamına girer (yol kendisiyle başlar)
- **Üst düğüm kapsam DIŞIDIR** — okul müdürü ilçe müdürlüğünün kaydını göremez
- Kardeş düğümler kapsam dışıdır
- Yolu boş olan aktör (geçiş koşmamış kayıt) **hiçbir şey** görür — bugünkü "kapsamsız aktör
  boş liste görür" kuralı korunur

`InstitutionScopePolicy` bugünkü eşitlik karşılaştırmasını bu kapsama sorusuyla değiştirir.
`InstitutionScopeGuardMiddleware` ve `GetInstitutionsHandler` aynı fonksiyonu çağırır;
ikinci bir yerde kimlik karşılaştırması yapılmaz.

---

## Roller ve izinler

Yeni roller: **`ProvincialAdmin`**, **`DistrictAdmin`**.

**Yeni izin eklenmez.** İkisi de düz `institution:view` alır; farkı izin değil, ağaçtaki
yeri yaratır. `institution:*` **verilmez** — o wildcard `InstitutionManager`'a aittir ve
`institution:` önekli her yeni izin sessizce her okul müdürüne geçer (ADR-0002 önek tuzağı).

`SystemAdmin` bugünkü dar hâlinde kalır: okul açar, ilk kullanıcısını bağlar, kurum verisine
girmez.

**A parçasında yazma yoktur.** İl/ilçe yetkilisi `institution:manage` almaz; arayüzdeki
yazma butonları `PermissionGuard` ile sarılı olduğu için kendiliğinden görünmez.

### Kapsam atama

- `SystemAdmin` → il yetkilisi atayabilir
- İl yetkilisi → **yalnız kendi ilinin** ilçe yetkililerini atayabilir

Kural ayrı yazılmaz, aynı fonksiyondan gelir: atanan düğümün yolu atayanın yoluyla
başlamalıdır. Böylece il yetkilisi ne başka ile, ne kendi üstüne yazabilir.

---

## Uçlar

| Uç | Değişiklik | İzin |
|---|---|---|
| `GET /api/institutions` | **Sayfalı olur** (`PagedQuery` → `PagedResult<InstitutionDto>`); süzgeçler: `nodeType` (varsayılan `School`), `parentId`, arama (ad + kurum kodu). Kapsam sorgunun içinde uygulanır | `institution:view` |
| `GET /api/institutions/{id}` | Guard eşitlik yerine yol kapsaması sorar | `institution:view` |
| `POST /api/institutions` | Gövde `nodeType` + `parentId` alır | `platform:tenant:manage` (değişmez) |
| `POST /api/institutions/rebuild-hierarchy` | Geçiş ucu (aşağıda) | `platform:tenant:manage` |

Liste ucunun sayfalı olması zorunlu: bugün sayfasız ve `List<InstitutionDto>` dönüyor; tek
okullu dünyada sorun değildi, il kapsamında yüzlerce satır demek.

`InstitutionDto` `nodeType`, `parentId`, `parentName` kazanır.
`/auth/me` yanıtı `institutionNodeType` kazanır.

---

## Arayüz

**Yeni:** `pages/institution/InstitutionListPage.vue`, rota `/institutions`.
`AppTable` + `useServerPagination`; sunucu tarafı arama/sıralama; ilçe süzgeci.
Menüdeki "Kurumlar" girdisi `/auth/me` → `institutionNodeType` değeri `School` **olmayan**
kullanıcıya gösterilir; okul kullanıcısına tek satırlık liste gösterilmez.

**Detay için yeni sayfa yazılmaz.** Satıra tıklama `/institutions/:id` rotasına gider ve
mevcut `InstitutionPage` açılır. Yazma butonları zaten `institution:manage` ile sarılı
olduğundan sayfa il yetkilisinde kendiliğinden salt okunur açılır.

`resolveEditableInstitutionId` sırası genişler: **rota parametresi → aktörün kendi kurumu →
yok**. (Bu fonksiyon 27.08.2026'da, sayfanın sıralamasız listenin ilk satırını düzenlemesi
hatası için eklendi; ağaçla birlikte üçüncü bir girdi kazanır.)

---

## Geçiş (dağıtım ön koşulu)

`POST /api/institutions/rebuild-hierarchy` mevcut okulların `ProvinceCode` / `DistrictName`
alanlarından il ve ilçe düğümlerini üretir, `ParentId` ve `Path` yazar. **İdempotent**:
ikinci koşu aynı ağacı üretir, düğüm çoğaltmaz.

**Atlanırsa sessizdir:** yollar boş kalır, `StartsWith` hiçbir şeyle eşleşmez ve sonuç hata
değil **boş liste** olur. `src/Docs/docs/infrastructure/dagitim-on-kosullari.md` dosyasına
zorunlu adım olarak eklenir.

---

## Testler

**Birim — `InstitutionScopePolicy` doğruluk tablosu:** kendisi, alt düğüm, torun düğüm,
kardeş, **üst düğüm (erişim yok)**, platform muafiyeti, boş yol, yol öneki benzerliği tuzağı
(`/33/1` ile `/33/10` karışmamalı — ayraçla biten önek karşılaştırması).

**Drift — `InstitutionNodeTypeDriftTests`:** "okul listesi" üreten her sorgu `NodeType`
süzmek zorundadır. Süzmeyen sorgu il/ilçe düğümünü okul sanar ve bu sessizce olur
(açılır listede bir MEM adı belirir, kimse hata görmez).

**Drift — `InstitutionScopeDriftTests` (mevcut) genişler:** kurum kimliğini elle
karşılaştıran uç kalmamalı; karar politikadan geçmeli.

**Geçiş testi:** aynı backfill iki kez koşunca aynı ağaç; il/ilçe bilgisi eksik okul için
davranış tanımlı (kök altına değil, **kapsamsız** kalır — yolu boş, kimse görmez, log'lanır).

**Ön yüz:** liste sayfası sayfalama/arama testi; `resolveEditableInstitutionId` rota
parametresi önceliği testi.

---

## Bilinen bedeller

1. **`Institution` artık "okul" demek değil.** Okul listesi/açılır listesi üreten her yer
   `NodeType == School` süzmek zorunda. Bu bir tarama işidir ve drift testiyle kilitlenir.
2. **Geçiş zorunlu ve sessiz başarısız olur** — yukarıdaki ön koşul.
3. **Düğüm taşınırsa** alt ağacın yolları yeniden yazılmalı. Nadir; tek yerde ve işlemsel
   olmalı. A parçasında taşıma ucu **yoktur** (yazma yok).
4. **"Benim kurumum" anlamı okul-üstü kullanıcı için değişir**; `institutionStore` ve marka
   teması bu kullanıcıda kendi düğümünün (İl MEM) verisini yükler. **A parçasında karar
   şudur:** il/ilçe düğümü palet seçmez; `BrandPaletteName` boş kalır ve `Resolve` zaten
   varsayılana (Mührü Lacivert) düşürür — yani il yetkilisi varsayılan temayı görür. Alt
   okulun paletinin üst düğüme ya da tersine miras olup olmayacağı B parçasının konusudur.

---

## Sonraki parçalar (bu spec'in dışında)

- **B — aktif bağlam değiştirme + yazma yetkisi.** Üst barda kurum seçici (Dönem/Yarıyıl'ın
  üstünde). Gizli maliyeti akademik dönem: kurum değişince dönem listesi yeniden
  yüklenmezse A okulunun dönem kimliğiyle B okuluna yazma olur.
- **C — denetim izi.** Depoda bugün **hiç yok** (`audit` geçen tek dosya bile yok). Wolverine
  middleware + yeni belge tipi + kiracılık sınıflandırması + "Son İşlemler" ekranı.

**Sıra bağlayıcıdır: C, B'den önce.** Tam yetki, izi olmadan verildiğinde bir kişi bütün
okulların kiracı sınırını taşır ve hiçbir kayıt kalmaz.
