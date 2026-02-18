FROM nginx:1.27-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf

# SSL sertifikaları için dizin (volume ile mount edilir)
RUN mkdir -p /etc/nginx/ssl

EXPOSE 80 443

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -q --spider http://localhost/ || exit 1
