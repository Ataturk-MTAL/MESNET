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

### Strateji: Ayrı Nginx Container

Frontend ve backend ayrı container'larda çalışır.

```
                   ┌─────────────┐
İnternet  ──────►  │  Nginx Rev  │
                   │  Proxy      │
                   └──────┬──────┘
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
   ┌──────────────────┐   ┌────────────────────┐
   │  Nginx (Frontend)│   │  MESNET.Presentation│
   │  static dist/    │   │  .NET 10 API        │
   │  port: 80/443    │   │  port: 8080          │
   └──────────────────┘   └────────────────────┘
```

### Frontend Dockerfile

```dockerfile
# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: Serve
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

### Nginx Konfigürasyonu (SPA Routing)

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    # Vue Router history mode — tüm route'ları index.html'e yönlendir
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy — aynı origin'den farklı container'a
    location /api/ {
        proxy_pass http://mesnet-api:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # SSE için uzun bağlantı
    location /notifications/ {
        proxy_pass http://mesnet-api:8080/notifications/;
        proxy_set_header Connection '';
        proxy_http_version 1.1;
        chunked_transfer_encoding on;
        proxy_buffering off;
        proxy_cache off;
        proxy_read_timeout 3600s;
    }
}
```

### docker-compose.yml

```yaml
services:
  frontend:
    build:
      context: ./src/WebUI
    ports:
      - "3000:80"
    environment:
      - VITE_API_URL=/api   # Nginx proxy üzerinden
    depends_on:
      - api

  api:
    build:
      context: .
      dockerfile: src/MESNET.Presentation/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__mesnet=Host=postgres;...
    depends_on:
      - postgres
      - rabbitmq
      - keycloak
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

> Nginx'te SSE için proxy_buffering ve chunked transfer ayarları kritiktir (yukarıda nginx.conf'ta mevcut).

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
