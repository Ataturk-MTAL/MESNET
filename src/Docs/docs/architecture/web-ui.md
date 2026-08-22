---
title: Web UI
---

# MESNET WebUI — Mimari Tasarım ve Geliştirme Planı

## Genel Bakış

MESNET'in frontend katmanı Vue 3 + Quasar ile geliştirilir ve `src/WebUI/` dizininde yaşar.
Backend API'lere yalnızca `src/MESNET.Presentation/` üzerinden erişilir.

---

## Teknoloji Yığını

| Katman | Teknoloji | Notlar |
|--------|-----------|--------|
| Framework | Vue 3 (Composition API) | `<script setup>` tercih edilir |
| UI Bileşen Kütüphanesi | Quasar Framework | `@quasar/vite-plugin` ile entegre |
| Build Aracı | Vite | `@quasar/vite-plugin` eklentisi |
| State Yönetimi | Pinia | Her modül için ayrı store |
| Tip Güvenliği | TypeScript | Strict mod |
| Auth | OIDC PKCE | `oidc-client-ts` veya `keycloak-js` (bkz. §Auth) |
| HTTP İstemcisi | Axios veya native fetch | API modülleri ayrı dosyalarda |
| Router | Vue Router 4 | History mode |

---

## Proje Yapısı

```
src/WebUI/
├── public/                     # Statik dosyalar (favicon, robots.txt)
├── src/
│   ├── assets/                 # Görseller, fontlar, global CSS
│   ├── boot/                   # Quasar boot dosyaları (axios, auth, i18n)
│   │   ├── auth.ts             # OIDC başlatma + token yenileme
│   │   └── axios.ts            # Axios instance + interceptor
│   ├── components/             # Paylaşılan bileşenler
│   │   ├── PermissionGuard.vue # İzin tabanlı render guard
│   │   └── layout/            # AppLayout, Navbar, Sidebar
│   ├── composables/            # Vue composable'ları (usePermission, useApi)
│   ├── pages/                  # Vue Router sayfa bileşenleri
│   │   ├── auth/               # Login, callback, logout sayfaları
│   │   ├── institution/        # Kurum yönetimi
│   │   ├── business/           # İşletme yönetimi
│   │   ├── enrollment/         # Başvuru ve kayıt
│   │   ├── contract/           # Sözleşme yönetimi
│   │   ├── attendance/         # Devamsızlık takibi
│   │   ├── payment/            # Ödeme/dekont
│   │   ├── coordination/       # Koordinasyon (ziyaret, değerlendirme)
│   │   └── reporting/          # Raporlar ve PDF indirme
│   ├── router/
│   │   └── index.ts            # Route tanımları + navigation guard
│   ├── stores/                 # Pinia store'ları
│   │   ├── auth.ts             # Kimlik doğrulama ve izin state'i
│   │   └── notifications.ts    # SSE bildirim state'i
│   ├── api/                    # Backend API çağrıları (modül bazlı)
│   │   ├── institution.ts
│   │   ├── business.ts
│   │   ├── enrollment.ts
│   │   └── ...
│   └── utils/
│       └── permissions.ts      # İzin kontrol yardımcı fonksiyonları
├── index.html
├── package.json
├── vite.config.ts
├── tsconfig.json
└── .env.development            # Geliştirme ortamı env değişkenleri
```

---

## Quasar Entegrasyon Yöntemi

Quasar CLI yerine **Vite Plugin** (`@quasar/vite-plugin`) kullanılır.
Bu yöntem Aspire ile NodeApp entegrasyonunu kolaylaştırır.

```ts
// vite.config.ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { quasar, transformAssetUrls } from '@quasar/vite-plugin'

export default defineConfig({
  plugins: [
    vue({ template: { transformAssetUrls } }),
    quasar({
      sassVariables: 'src/assets/quasar-variables.sass'
    })
  ],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5270',
        changeOrigin: true
      },
      '/notifications': {
        target: 'http://localhost:5270',
        changeOrigin: true,
        ws: true   // SSE + WebSocket için
      }
    }
  }
})
```

---

## Kimlik Doğrulama (Auth)

### Strateji: SPA PKCE Flow

Backend `AddKeycloakWebApiAuthentication` ile JWT Bearer doğrulaması yapıyor.
Frontend Keycloak'a doğrudan **Authorization Code + PKCE** akışıyla bağlanır.

```
Kullanıcı → Quasar SPA
         → Keycloak /auth (PKCE)
         ← access_token + refresh_token
         → API istekleri: Authorization: Bearer <access_token>
         → Backend: JWT doğrulama (Keycloak public key)
```

### Neden Cookie Tabanlı BFF Değil?

BFF (Backend-for-Frontend) pattern HttpOnly cookie kullanır ve XSS'e karşı daha güvenlidir.
Ancak `MESNET.Presentation`'da `/bff/login`, `/bff/user`, `/bff/logout` endpoint'leri eklenmesini gerektirir.
Bu backend değişikliği ayrıca ele alınacaktır — mevcut aşamada **PKCE tercih edilir**.

> **Kararı yeniden gözden geçir:** BFF ihtiyacı doğarsa backend ajanıyla koordineli implementasyon gerekir.

### Token Yönetimi

- Access token: in-memory (Pinia store) — XSS erişimine karşı
- Refresh token: `sessionStorage` — sekme kapanınca temizlenir
- Otomatik yenileme: `oidc-client-ts` `automaticSilentRenew: true`
- Logout: Keycloak session sonlandırma + store temizleme

### İzin Kontrolü

Keycloak token içindeki `resource_access.mesnet.roles` alanından izinler parse edilir.

```ts
// stores/auth.ts
const permissions = payload.resource_access?.['mesnet']?.roles ?? []
```

`PermissionGuard.vue` bileşeni ve `usePermission()` composable ile route/bileşen düzeyinde kontrol.

---

## Aspire + Frontend Entegrasyonu (Yerel Geliştirme)

### Yöntem: Aspire NodeApp

Quasar/Vite dev server'ı Aspire AppHost içinden **NodeApp** olarak başlatılır.
Böylece Aspire dashboard'dan takip edilir ve servisler arasındaki URL'ler otomatik inject edilir.

```csharp
// MESNET.AppHost/Program.cs — eklenecek
var frontend = builder
    .AddNpmApp("frontend", "../../src/WebUI")
    .WithHttpEndpoint(port: 5173, env: "VITE_PORT")
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"));

builder.AddProject<Projects.MESNET_Presentation>("mesnet-api")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(keycloak)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitFor(minio);
```

> **NOT:** AppHost'taki değişiklikler backend ajana aittir. Yukarıdaki kod referans amaçlıdır.

### Geliştirme Ortamı Env Dosyası

```ini
# src/WebUI/.env.development
VITE_API_URL=http://localhost:5270
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=mesnet
VITE_KEYCLOAK_CLIENT_ID=mesnet-frontend
```

### Vite Proxy

Geliştirmede Vite proxy, frontend'in `http://localhost:5270`'deki API'ye `/api` prefix'iyle ulaşmasını sağlar.
CORS sorunu yaşanmaz — tarayıcı her şeyi `localhost:5173`'e istek atıyormuş gibi görür.

---

## Production Deployment

### Strateji: Tek Caddy Container (static + reverse proxy)

Caddy tek başına hem frontend/docs static dosyalarını sunar hem de reverse proxy görevi görür.
Let's Encrypt ile **otomatik HTTPS** (ACME) — manuel sertifika yönetimi yok. Alan adları:

- `APP_DOMAIN` — Vue SPA (+ `/api/*` → backend)
- `docs.APP_DOMAIN` — Docusaurus dokümantasyon sitesi (static)
- `auth.APP_DOMAIN` — Keycloak (kimlik doğrulama)

```
                        ┌──────────────────────────┐
İnternet  ───(443)────► │          Caddy           │
                        │  otomatik HTTPS (ACME)    │
                        │  /srv/web  (Vue SPA)      │
                        │  /srv/docs (Docusaurus)   │
                        └───┬──────────┬──────────┬─┘
                  /api/*    │   auth.*  │  docs.*  │ (static)
                            ▼           ▼
                 ┌────────────────┐  ┌──────────────┐
                 │ MESNET API      │  │  Keycloak    │
                 │ .NET 10 :8080   │  │  :8080       │
                 └────────────────┘  └──────────────┘
```

Keycloak ve API dış dünyaya **doğrudan port açmaz** — yalnız iç ağdan erişilir, Caddy önlerinde durur.

### Caddy imajı (multi-stage build)

Tek Dockerfile web ve docs'u build edip Caddy imajına gömer — kaynak: `src/caddy/Dockerfile`.

```dockerfile
# Stage 1: Web (Vue SPA) build → /web/dist
FROM node:22-alpine AS web
RUN corepack enable && corepack prepare pnpm@9.15.4 --activate
WORKDIR /web
COPY src/WebUI/package.json src/WebUI/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/WebUI/ ./
RUN pnpm build

# Stage 2: Docs (Docusaurus) build → /docs/build
FROM node:22-alpine AS docs
RUN npm i -g pnpm@9.15.4
WORKDIR /docs
COPY src/Docs/package.json src/Docs/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/Docs/ ./
RUN pnpm build

# Stage 3: Caddy — static'i sun + proxy'le
FROM caddy:2-alpine
COPY --from=web /web/dist /srv/web
COPY --from=docs /docs/build /srv/docs
COPY src/caddy/Caddyfile /etc/caddy/Caddyfile
```

> **Vite public config build zamanında gömülür.** SPA `import.meta.env.VITE_KEYCLOAK_URL`
> ile Keycloak'a bağlanır; bu değer runtime'da değişmez. Web stage `VITE_KEYCLOAK_URL`
> (= `https://auth.${APP_DOMAIN}`), `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID` build
> ARG'larını alıp `.env.production`'a yazar. API adresi `/api` (same-origin) olduğu için
> domain'e bağlı değildir. docker-compose bu ARG'ları `.env`'den geçirir; CI yayın imajı
> repo değişkenlerinden.

### Caddyfile (SPA routing + proxy + SSE)

Kaynak: `src/caddy/Caddyfile`. SPA fallback `try_files … /index.html`, SSE için
`flush_interval -1` (anlık aktarım, buffering yok).

```caddyfile
{$APP_DOMAIN} {
    encode gzip zstd

    # SSE — bildirim akışı (anlık flush)
    handle /api/notifications/stream {
        reverse_proxy api:8080 {
            flush_interval -1
        }
    }
    handle /api/* {
        reverse_proxy api:8080
    }
    # Vue SPA — client-side routing fallback
    handle {
        root * /srv/web
        try_files {path} /index.html
        file_server
    }
}

docs.{$APP_DOMAIN} {
    root * /srv/docs
    file_server
}

auth.{$APP_DOMAIN} {
    reverse_proxy keycloak:8080
}
```

### docker-compose.yml

`APP_DOMAIN` + `ACME_EMAIL` env'den gelir (`.env.example`'a bak). Caddy sertifikaları
`caddy-data` volume'unda kalıcıdır (yeniden başlatmada yeniden almaz).

```yaml
services:
  caddy:
    build:
      context: .
      dockerfile: src/caddy/Dockerfile
    ports: ["80:80", "443:443"]
    environment:
      APP_DOMAIN: ${APP_DOMAIN}
      ACME_EMAIL: ${ACME_EMAIL}
    volumes:
      - caddy-data:/data
      - caddy-config:/config
    depends_on: [api, keycloak]

  api:
    build:
      context: .
      dockerfile: src/MESNET.Presentation/Dockerfile
    expose: ["8080"]   # dışa port yok — Caddy /api/* ile proxy'ler
    depends_on: [postgres, rabbitmq, keycloak]
```

---

## SSE (Server-Sent Events) Entegrasyonu

Backend `/notifications/stream` endpoint'i SSE ile anlık bildirim gönderiyor.
Frontend bağlantıyı `EventSource` API ile açar.

```ts
// stores/notifications.ts
export const useNotificationStore = defineStore('notifications', () => {
  const messages = ref<Notification[]>([])

  function connect(token: string) {
    const source = new EventSource(
      `/notifications/stream?access_token=${token}`
    )
    source.onmessage = (e) => {
      messages.value.push(JSON.parse(e.data))
    }
    source.onerror = () => {
      // Yeniden bağlanma mantığı
    }
    return source
  }

  return { messages, connect }
})
```

> Caddy'de SSE için `reverse_proxy … { flush_interval -1 }` kritiktir — buffering'i kapatıp
> her mesajı anında istemciye aktarır (yukarıdaki Caddyfile'da `/api/notifications/stream` bloğu).

---

## Modül-Sayfa Eşlemesi

| Backend Modülü | Frontend Sayfa Grubu | Ana İşlemler |
|----------------|---------------------|--------------|
| Institution | `/institution/` | Kurum bilgileri, alan katalogu |
| Business | `/business/` | İşletme ekleme, belge yükleme, eğitici |
| Enrollment | `/enrollment/` | Başvuru, yerleştirme, öğrenci/öğretmen listesi |
| Contract | `/contract/` | Sözleşme oluşturma, imzalama, fesih |
| Attendance | `/attendance/` | Devamsızlık girişi, takvim |
| Payment | `/payment/` | Dekont yükleme, onay akışı |
| Coordination | `/coordination/` | Ziyaret, değerlendirme, rapor, sınav |
| Internship | `/internship/` | Staj yaşam döngüsü özeti (saga) |
| Reporting | `/reporting/` | PDF indirme, rapor listesi |
| Security | `/admin/` | Kullanıcı yönetimi, davetiyeler, roller |

---

## Geliştirme Sırası (Öneri)

1. **Proje Kurulumu** — package.json, vite.config.ts, tsconfig.json, Quasar boot
2. **Auth Altyapısı** — OIDC PKCE, Pinia auth store, router guard
3. **Layout ve Navigasyon** — AppLayout, Sidebar, izin tabanlı menü
4. **Institution Modülü** — En bağımsız modül, CRUD sayfaları
5. **Business Modülü** — Belge yükleme (MinIO), konum seçici
6. **Enrollment Modülü** — Başvuru formu, yerleştirme
7. **Contract Modülü** — Form ağırlıklı, durum takibi
8. **Attendance + Payment** — Tablo ağırlıklı
9. **Coordination** — Takvim bileşeni gerekebilir
10. **Reporting** — PDF indirme, filtre panelleri
11. **Admin (Security)** — Kullanıcı ve davetiye yönetimi
12. **SSE Bildirimleri** — Son aşamada entegre

---

## Henüz Kararlaştırılmamış Konular

- [ ] **Auth yöntemi:** PKCE mi kalacak yoksa BFF (HttpOnly cookie) mı değerlendirilecek?
- [ ] **i18n:** Türkçe UI metinleri için `vue-i18n` eklenecek mi?
- [ ] **Test:** Vitest (unit) + Playwright (e2e) planlanıyor mu?
- [ ] **Aspire NodeApp:** AppHost değişikliği backend ajanıyla koordineli yapılacak
