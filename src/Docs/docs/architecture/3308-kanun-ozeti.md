---
title: 3308 Sayılı Kanun — Özet Notlar
---

# 3308 Sayılı Mesleki Eğitim Kanunu — Ücret ve Devlet Katkısı Özeti

Kaynak: [mevzuat.gov.tr — 1.5.3308.pdf](https://www.mevzuat.gov.tr/MevzuatMetin/1.5.3308.pdf)
(22 sayfa, birleştirilmiş metin). Bu doküman kanunun **tamamının** özeti değildir; yalnız
MESNET'in para hesabını ilgilendiren maddeleri kapsar.

:::warning Bu doküman kanun metninin yerine geçmez
Aşağıdaki oranlar `SalaryCalculationConfig` içindeki sabitlerin **dayanağıdır**. Mevzuat
değişirse önce bu doküman, sonra kod güncellenmelidir. Uyuşmazlıkta kanun metni esastır.
:::

## Madde 25 — İşletmenin ödeyeceği ücret (taban)

> Ücret ve artışlar **sözleşme ile tespit edilir.** Ancak aşağıdaki tabanların altına inilemez.

| Kim / nerede | Taban |
|---|---|
| **20 ve üzerinde** personel çalıştıran işyeri | Net asgari ücretin **%30**'u |
| **20'den az** personel çalıştıran işyeri | Net asgari ücretin **%15**'i |
| Aday çırak ve çırak | Yaşına uygun asgari ücretin **%30**'u |
| Kalfalık yeterliği kazanan **MEM 12. sınıf** öğrencisi | Asgari ücretin **%50**'si |

Kapsam dışı: staj yapacak işletme bulunamadığı için stajını **okulda** yapan ortaöğretim
öğrencileri ve yükseköğretim kurumlarında yapılan stajlar.

Diğer hükümler:

- Ödenen ücretler **her türlü vergiden müstesnadır**
- Sigorta primleri **asgari ücretin %50'si üzerinden**, Bakanlık bütçesindeki ödenekten karşılanır
- İşyerinin kusuru hâlindeki iş kazası ve meslek hastalığından **işveren sorumludur**

**Not:** Sözleşme ücreti tabandan yüksek olabilir ve o zaman esas alınan sözleşme ücretidir —
kanun taban belirler, tavan değil. MESNET bunu `StudentContractWageView` ile temsil eder.

## Geçici Madde 12 — Devlet katkısı (teşvik)

İşletmeye ödenen destek. Oranlar **ödenebilecek en az ücret** üzerinden hesaplanır:

| Öğrenci / işletme | Devlet katkısı |
|---|---|
| MEM programı **dışındaki** okul öğrencisi, **20'den az** personelli işletme | En az ücretin **2/3**'ü |
| MEM programı **dışındaki** okul öğrencisi, **20 ve üzeri** personelli işletme | En az ücretin **1/3**'ü |
| **MEM programına** devam eden öğrenci | En az ücretin **tamamı** |

Kaynak: 4447 sayılı İşsizlik Sigortası Kanunu m. 53/3-(B)-(h) için ayrılan tutar.

### ⚠️ Kamu kurumlarına devlet katkısı ÖDENMEZ

> "Kamu kurum ve kuruluşlarına Devlet katkısı ödenmez."

Kapsam dışı olanlar (katkı yok): işletme bulunamadığı için stajını okulda yapan ortaöğretim
öğrencileri; öğretim programı gereği staj yapmak zorunda olmayan yükseköğretim öğrencileri.

### Süre sınırı — kalıcı değil

Geçici Madde 12 metni "2016-2017 eğitim ve öğretim yılı sonuna kadar uygulanmak üzere" der;
uzatma yetkisi Cumhurbaşkanı'ndadır ("on eğitim ve öğretim yılına kadar uzatmaya yetkilidir").

**Bakım riski:** devlet katkısı süreli bir düzenlemedir ve dönemsel kararlarla uzatılır.
Kodda kalıcı bir hak gibi ele alınmamalı; uzatma kararı takip edilmelidir.

Geriye dönük ödeme yasağı: 2021-2022 eğitim öğretim yılından önceki döneme ilişkin geçmişe
dönük ücret veya devlet katkısı ödemesi **yapılmaz**.

## Kod ile karşılaştırma

`src/Modules/Payment/MESNET.Payment.Core/Entities/SalaryCalculationConfig.cs` içindeki
varsayılanlar kanunla **birebir uyuşuyor** (doğrulama tarihi: 2026-07-29):

| Kod alanı | Değer | Kanun |
|---|---|---|
| `ApprenticeRate` | `0.30` | ✅ Madde 25 — aday çırak/çırak %30 |
| `PersonnelThreshold` | `20` | ✅ Madde 25 — eşik 20 personel |
| `LargeBusinessRate` | `0.30` | ✅ Madde 25 — 20+ personel %30 |
| `SmallBusinessRate` | `0.15` | ✅ Madde 25 — 20'den az %15 |
| `MEM12thGradeRate` | `0.50` | ✅ Madde 25 — MEM 12. sınıf %50 |
| `GovContribSmallNonMEM` | `2m / 3m` | ✅ Geçici M.12 — 20'den az: 2/3 |
| `GovContribLargeNonMEM` | `1m / 3m` | ✅ Geçici M.12 — 20+: 1/3 |
| `GovContribMEM` | `1.0` | ✅ Geçici M.12 — MEM: tamamı |

## Kanunun CEVAPLAMADIĞI ve kodda EKSİK olanlar

### 1. Ay içi fesih / kısmi ay — kanun sessiz

**Ne Madde 25 ne Geçici Madde 12 kısmi aydan söz eder.** Öğrenci ay ortasında işletme
değiştirdiğinde ücretin ve devlet katkısının nasıl bölüşüleceği kanun metninde **yoktur**.

Geçici Madde 12 son fıkra: *"Bu maddenin uygulanmasına ilişkin usul ve esaslar Bakanlık ve
Türkiye İş Kurumu tarafından belirlenir."*

Yani cevap **ikincil mevzuattadır** (MEB genelgesi / İŞKUR usul ve esasları), kanunda değil.
Kanun metnini okumak bu soruyu kapatmaz.

İlgili: [#154](https://github.com/Ataturk-MTAL/MESNET/issues/154) — sistem bugün tam ay
varsayıyor ve iki işveren arasında bölüşme yapamıyor.

### 2. Kamu kurumu ayrımı kodda uygulanmamış

`GovernmentContributionType.PublicInstitution` enum değeri **tanımlı ama hiçbir yerde
kullanılmıyor** — hiçbir kod onu atamıyor ya da kontrol etmiyor.

Sonuç: kamu kurumunda staj yapan öğrenci için devlet katkısı, kanun ödenmemesini emrettiği
hâlde özel işletme gibi hesaplanır.

### 3. Sigorta primi kapsam dışı

Madde 25'e göre sigorta primleri asgari ücretin %50'si üzerinden **Bakanlık bütçesinden**
karşılanır. MESNET bu akışı temsil etmiyor; kapsam dışı olması bilinçli mi, karar verilmeli.

## Diğer ilgili maddeler (özet)

| Madde | Konu |
|---|---|
| **Madde 15** | Aday çırak/çırak almak için işyerinde **usta öğretici bulunması şarttır** |
| **Madde 20** | İşletmelerde beceri eğitimi gören öğrencilerle ilgili hükümler |
| **Madde 24** | 10 ve daha fazla personel çalıştıran işletmeler için yükümlülük |
| **Madde 26** | Her yıl tatil aylarında **bir ay ücretli izin**; mazerete bağlı bir aya kadar ücretsiz izin (okul müdürlüğü görüşüyle) |
| **Madde 38** | 20 ve daha fazla personel çalıştıran işletmeler — personel oranına bağlı yükümlülük |

**Madde 15 notu:** usta öğretici şartı, bir işletmenin yeni öğrenci alamaz hâle gelmesinin
yasal dayanağıdır — `BusinessStatus.Inactive` durumunun gerekçesi (bkz.
[#147](https://github.com/Ataturk-MTAL/MESNET/issues/147)).

**Madde 26 notu:** ücretli izin ayının maaş hesabına etkisi kontrol edilmemiştir.
