# MESNET — Caddy imajı: Vue SPA + Docusaurus static'i build edip Caddy ile sunar.
# Build context = repo kökü (web ve docs kaynaklarına erişmek için).

# ── Stage 1: Web (Vue SPA / Vite) build ──
FROM node:22-alpine AS web
RUN corepack enable && corepack prepare pnpm@9.15.4 --activate
WORKDIR /web

# Vite public config'i BUILD zamanında bundle'a gömer (runtime'da değişmez).
# Yalnız Keycloak URL domain'e bağlı; realm/clientId sabit, API '/api' (same-origin).
ARG VITE_KEYCLOAK_URL=https://auth.localhost
ARG VITE_KEYCLOAK_REALM=mesnet
ARG VITE_KEYCLOAK_CLIENT_ID=mesnet-web
ARG VITE_OSRM_URL=

COPY src/WebUI/package.json src/WebUI/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/WebUI/ ./
# ARG'ları .env.production'a yaz — vite build (production mode) bunları okur.
# (Shell env'e güvenmeyip dosyaya yazmak Vite sürümünden bağımsız deterministiktir.)
RUN printf 'VITE_KEYCLOAK_URL=%s\nVITE_KEYCLOAK_REALM=%s\nVITE_KEYCLOAK_CLIENT_ID=%s\nVITE_OSRM_URL=%s\n' \
      "$VITE_KEYCLOAK_URL" "$VITE_KEYCLOAK_REALM" "$VITE_KEYCLOAK_CLIENT_ID" "$VITE_OSRM_URL" \
      > .env.production
RUN pnpm build          # → /web/dist

# ── Stage 2: Docs (Docusaurus) build ──
FROM node:22-alpine AS docs
RUN npm i -g pnpm@9.15.4
WORKDIR /docs
COPY src/Docs/package.json src/Docs/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/Docs/ ./
RUN pnpm build          # → /docs/build

# ── Stage 3: Caddy (serve + reverse proxy) ──
FROM caddy:2-alpine
COPY --from=web /web/dist /srv/web
COPY --from=docs /docs/build /srv/docs
COPY src/caddy/Caddyfile /etc/caddy/Caddyfile
EXPOSE 80 443
