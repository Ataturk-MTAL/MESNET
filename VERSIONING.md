# Sürümleme Kuralı (Versioning)

MESNET, [Anlamsal Sürümleme (SemVer)](https://semver.org/lang/tr/) `vMAJOR.MINOR.PATCH` üzerine
**özel bir kanal kuralı** ekler.

## Kanal kuralı — minör sürümün PARİTESİ

| Minör sürüm | Kanal | Örnek |
|---|---|---|
| **Tek (odd)** | **Ön-sürüm (pre-release)** | `v0.1.0`, `v0.3.0`, `v1.1.0`, `v2.5.0` |
| **Çift (even)** | **Kararlı sürüm (release)** | `v0.2.0`, `v0.4.0`, `v1.2.0`, `v2.6.0` |

- `0.1.0` bir **ön-sürümdür**; onu izleyen ilk **kararlı sürüm** `0.2.0` olur.
- Mantık: tek minörler aktif geliştirme/deneme hattı, çift minörler stabilize edilmiş yayınlardır.

## Branch & etiket akışı

- Geliştirme `dev` branch'inde yapılır.
- Her sürüm için `vX.Y.Z` adında bir **branch** + aynı isimde bir **git tag** açılır.
- **GitHub Release:**
  - Tek minör → `--prerelease` (ön-sürüm) işaretli.
  - Çift minör → tam (kararlı) release.
- `main`, **kararlı (çift minör)** sürüm hattıdır.

## Sürüm geçmişi

| Sürüm | Kanal | Kapsam |
|---|---|---|
| `v0.1.0` | ön-sürüm | İlk ön-sürüm — Phase 1 çekirdek staj süreçleri; BDD API test suite (228 test); 46 sunucu-hatası + 12 mimari + 8 UX düzeltmesi. |
