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
- Sürüm: `dev → main` **Pull Request ile** birleştirilir.
- Birleşme sonrası **git tag** (`vX.Y.Z`) **`main`** üzerinde açılır.
- **GitHub Release** bu tag'den oluşturulur:
  - **Tek minör** → `--prerelease` (ön-sürüm) işaretli.
  - **Çift minör** → tam (kararlı) release.
- `main`, tüm sürümlerin PR ile birleştiği hattır; minör parite yalnızca Release'in
  ön-sürüm/kararlı işaretini belirler.

## Konteyner imajları

- Bir tag (`v*`) push edildiğinde **GitHub Actions** deploy edilebilir bileşenlerin imajlarını
  derler ve **GHCR'ye** (`ghcr.io/ataturk-mtal/mesnet-*`) push eder: `mesnet-api`, `mesnet-caddy`.
- **Caddy göçü:** eski `mesnet-web`, `mesnet-nginx` ve `mesnet-docs` imajları kaldırıldı. Artık
  tek bir `mesnet-caddy` imajı Vue SPA + Docusaurus static içeriğini gömer ve reverse proxy'yi
  üstlenir.
- Depo **private** olduğundan imajlar da **private**'tır — **public yayınlanmaz**.
- İş akışı: `.github/workflows/release-containers.yml`.

## Dev imaj kanalı (rolling)

Sürüm (release) akışından **bağımsız**, sürekli güncellenen bir geliştirme kanalıdır.

- `dev` branch'ine **her push'ta** `mesnet-api-dev` ve `mesnet-caddy-dev` imajları derlenip
  GHCR'ye push edilir. İki etiket üretilir:
  - `:dev` — **mutable**, her push'ta son commit'e taşınır (hep en güncel).
  - `:sha-<short>` — o commit'in kısa hash'ine **sabit** kalıcı etiket.
- **Ayrı paket adları (`-dev` soneki):** dev imajları, sürüm imajlarından (`mesnet-api`,
  `mesnet-caddy`) **ayrı** paketlerde tutulur. Böylece dev retention temizliği yayınlanmış
  sürüm imajlarına **asla dokunmaz**.
- **Git tag veya GitHub Release OLUŞMAZ** — bu kanal yalnızca GHCR imajı üretir.
- İki etiket aynı image digest'ine yapıştığı için push başına **tek** paket-versiyonu oluşur;
  eski versiyon `:dev` etiketini kaybeder ama `:sha-<short>` ile etiketli kalır.
- **Retention:** her `-dev` paketi için en yeni **~8** sürüm tutulur, eskiler otomatik silinir
  (manifest-aware temizlik; canlı `:dev` etiketi silmeden korunur).
- İş akışı: `.github/workflows/dev-images.yml`.
- **Ön koşul (retention için):** org paketinde silme yetkisi — paket ayarlarından
  (Package → Manage Actions access) bu repoya **Admin** rolü verilmeli; yoksa temizlik 403
  alır (imaj build/push etkilenmez).
- **Milestone (SemVer-parite) akışı DEĞİŞMEDİ** — kararlı/ön-sürüm imajları yalnızca `v*` tag
  push'unda `release-containers.yml` ile üretilir.

## Sürüm geçmişi

| Sürüm | Kanal | Kapsam |
|---|---|---|
| `v0.1.0` | ön-sürüm | İlk ön-sürüm — Phase 1 çekirdek staj süreçleri; BDD API test suite (228 test); 46 sunucu-hatası + 12 mimari + 8 UX düzeltmesi. |
