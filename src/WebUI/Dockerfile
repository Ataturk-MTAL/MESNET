# Stage 1: Build
FROM node:22-alpine AS build

# pnpm etkinleştir
RUN corepack enable && corepack prepare pnpm@latest --activate

WORKDIR /app

# Bağımlılık katmanını önce kopyala (Docker layer cache)
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# Kaynak kodu kopyala ve build
COPY . .
RUN pnpm run build

# Stage 2: Serve
FROM nginx:1.27-alpine

# Build çıktısını kopyala
COPY --from=build /app/dist /usr/share/nginx/html

# Nginx konfigürasyonu
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Varsayılan Nginx konfigürasyonunu kaldır (default.conf çakışmasını önler)
RUN rm /etc/nginx/conf.d/default.conf.bak 2>/dev/null || true

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -q --spider http://localhost/ || exit 1
