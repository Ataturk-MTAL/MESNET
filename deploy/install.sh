#!/usr/bin/env bash
#
# MESNET — sıfırdan üretim kurulumu.
#
# NE YAPAR
# --------
# Temiz bir sunucuda çalışan bir MESNET kurulumu bırakır: veri katmanı, Keycloak realm'i,
# uygulama ve ilk yönetici hesabı. Her faz İDEMPOTENTtir; yarıda kalan kurulum yeniden
# çalıştırılarak tamamlanır.
#
# NE YAPMAZ
# ---------
# Dağıtım ön koşullarını (resync/backfill uçları) KOŞTURMAZ. Onlar `scripts/deploy-prereqs.sh`
# işidir ve ayrı bir kimlikle koşar; bu betik sonunda o komutu yazar.
#
# REALM NEDEN IMPORT EDİLMİYOR
# ----------------------------
# Keycloak realm import TEK SEFERLİKTİR: dosyaya sonradan eklenen rol, politika ya da client
# ayakta duran bir kaba HİÇ ulaşmaz. Ölçüldü (#195): dev realm'inde depoda 11 rol tanımlıyken
# çalışan realm'de yalnız 6'sı vardı ve eksik beşi farklı sürümlerde eklenip her seferinde
# unutulmuştu. Bu betik realm'i Admin API ile YAKINSAR — her koşuda tekrarlanabilir, var olan
# realm'i bozmadan eksiği tamamlar.
#
# PAROLALAR
# ---------
# Bu betik hiçbir parola İÇERMEZ ve hiçbir parolayı ekrana yazmaz. Değerler `.env`'den okunur;
# ilk yöneticinin parolası terminalden sorulur. `mesnet-api` client secret'ını KEYCLOAK üretir,
# betik yalnız okuyup `.env`'e yazar — böylece iki taraf hiç ayrışmaz.
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"
REALM_TEMPLATE="$SCRIPT_DIR/keycloak/mesnet-realm.production.json"

DRY_RUN=0
SKIP_BOOTSTRAP=0
ONLY=""

usage() {
    cat <<'USAGE'
Kullanım: install.sh [seçenekler]

  --dry-run           Hiçbir şey değiştirilmez; plan ve ön kontroller yazılır
  --only <faz>        Yalnız o faz koşar: on-kontrol | veri | keycloak | uygulama | yonetici
  --skip-bootstrap    İlk yönetici hesabı açılmaz (hesap zaten varsa)
  -h, --help          Bu metin

Ön koşul: deploy/.env doldurulmuş olmalı (bkz. deploy/.env.example).
USAGE
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --dry-run) DRY_RUN=1; shift ;;
        --only) ONLY="${2:?--only bir faz adı ister}"; shift 2 ;;
        --skip-bootstrap) SKIP_BOOTSTRAP=1; shift ;;
        -h|--help) usage; exit 0 ;;
        *) echo "Bilinmeyen seçenek: $1" >&2; usage >&2; exit 2 ;;
    esac
done

# ── Çıktı ────────────────────────────────────────────────────────────────────
if [[ -t 1 ]]; then B=$'\033[1m'; K=$'\033[31m'; Y=$'\033[33m'; G=$'\033[32m'; N=$'\033[0m'
else B=""; K=""; Y=""; G=""; N=""; fi

faz()   { printf '\n%s══ %s %s\n' "$B" "$*" "$N"; }
bilgi() { printf '   %s\n' "$*"; }
iyi()   { printf '   %sTAMAM%s  %s\n' "$G" "$N" "$*"; }
uyar()  { printf '   %sUYARI%s  %s\n' "$Y" "$N" "$*"; }
oldu()  { printf '   %sHATA%s   %s\n' "$K" "$N" "$*" >&2; }
dur()   { oldu "$*"; exit 1; }

calisir() { [[ -z "$ONLY" || "$ONLY" == "$1" ]]; }

# ── compose sağlayıcısını tespit et ──────────────────────────────────────────
# Proje Podman kullanır; sunucuda Docker bulunabileceği için ikisi de kabul edilir.
COMPOSE=""
if command -v podman >/dev/null 2>&1 && podman compose version >/dev/null 2>&1; then
    COMPOSE="podman compose"
elif command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    COMPOSE="docker compose"
fi

dc() {
    if (( DRY_RUN )); then
        bilgi "[dry-run] $COMPOSE $*"
        return 0
    fi
    # shellcheck disable=SC2086
    (cd "$SCRIPT_DIR" && $COMPOSE --env-file "$ENV_FILE" -f compose.yml "$@")
}

# ═══════════════════════════════════════════════════════════════════════════
faz "1/5  Ön kontrol"
# ═══════════════════════════════════════════════════════════════════════════

for t in curl jq openssl; do
    command -v "$t" >/dev/null 2>&1 || dur "'$t' bulunamadı; kurup tekrar deneyin."
done
[[ -n "$COMPOSE" ]] || dur "Ne 'podman compose' ne 'docker compose' bulundu."
iyi "araçlar hazır — compose sağlayıcısı: $COMPOSE"

[[ -f "$ENV_FILE" ]] || dur "$ENV_FILE yok. Önce: cp .env.example .env && chmod 600 .env && \$EDITOR .env"
[[ -f "$REALM_TEMPLATE" ]] || dur "Realm şablonu yok: $REALM_TEMPLATE"

# .env izinleri — parola taşır.
perm="$(stat -f '%Lp' "$ENV_FILE" 2>/dev/null || stat -c '%a' "$ENV_FILE" 2>/dev/null || echo '?')"
if [[ "$perm" != "600" && "$perm" != "400" ]]; then
    uyar ".env izni $perm — parola taşıyan dosya için geniş. Önerilen: chmod 600 $ENV_FILE"
fi

set -a; # shellcheck disable=SC1090
source "$ENV_FILE"; set +a

zorunlu=(MESNET_VERSION APP_DOMAIN ACME_EMAIL POSTGRES_USER POSTGRES_PASSWORD
         RABBITMQ_USER RABBITMQ_PASSWORD MINIO_ROOT_USER MINIO_ROOT_PASSWORD
         KEYCLOAK_ADMIN_USER KEYCLOAK_ADMIN_PASSWORD SMTP_HOST SMTP_FROM_EMAIL)
eksik=()
for v in "${zorunlu[@]}"; do [[ -n "${!v:-}" ]] || eksik+=("$v"); done
(( ${#eksik[@]} == 0 )) || dur ".env içinde boş bırakılmış zorunlu değer: ${eksik[*]}"

# Örnek değerler kaldıysa kurulum yanlış alan adına gider.
case "$APP_DOMAIN" in
    *example.com|*example.gov.tr|localhost) dur "APP_DOMAIN hâlâ örnek değer: $APP_DOMAIN" ;;
esac
[[ "$MESNET_VERSION" != "latest" ]] || dur "MESNET_VERSION 'latest' olamaz — kararlı kurulum sabit etiket ister."
iyi ".env eksiksiz — sürüm $MESNET_VERSION, alan adı $APP_DOMAIN"

# DNS: üç ad da bu sunucuya gelmeli. auth eksikse giriş HİÇ tamamlanmaz.
for host in "$APP_DOMAIN" "auth.$APP_DOMAIN" "docs.$APP_DOMAIN"; do
    if getent hosts "$host" >/dev/null 2>&1 || host "$host" >/dev/null 2>&1; then
        iyi "DNS çözülüyor: $host"
    else
        uyar "DNS çözülmüyor: $host — TLS sertifikası alınamaz, giriş akışı tamamlanmaz."
    fi
done

if (( DRY_RUN )); then
    faz "dry-run — buradan sonrası çalıştırılmadı"
    exit 0
fi

# ═══════════════════════════════════════════════════════════════════════════
faz "2/5  Veri katmanı"
# ═══════════════════════════════════════════════════════════════════════════
if calisir veri; then
    dc pull postgres
    dc up -d postgres
    bilgi "PostgreSQL hazır olması bekleniyor…"
    for i in $(seq 1 60); do
        if dc exec -T postgres pg_isready -U "$POSTGRES_USER" -d mesnet >/dev/null 2>&1; then break; fi
        [[ $i -eq 60 ]] && dur "PostgreSQL 120 sn içinde hazır olmadı."
        sleep 2
    done
    iyi "PostgreSQL ayakta"

    # Keycloak kendi tablolarını AYRI şemaya yazar (compose: KC_DB_SCHEMA=keycloak). Şemayı
    # Keycloak KENDİ YARATMAZ; önceden var olmalıdır, yoksa açılışta düşer.
    dc exec -T postgres psql -U "$POSTGRES_USER" -d mesnet \
        -c 'CREATE SCHEMA IF NOT EXISTS keycloak;' >/dev/null
    iyi "keycloak şeması hazır (uygulama şemalarından ayrı)"
fi

# ═══════════════════════════════════════════════════════════════════════════
faz "3/5  Keycloak ve realm"
# ═══════════════════════════════════════════════════════════════════════════
AUTH_URL="https://auth.${APP_DOMAIN}"
REALM="${KEYCLOAK_REALM:-mesnet}"
API_CLIENT="${KEYCLOAK_CLIENT_ID:-mesnet-api}"

kc_token() {
    curl -fsS -X POST "$AUTH_URL/realms/master/protocol/openid-connect/token" \
        -d grant_type=password -d client_id=admin-cli \
        --data-urlencode "username=$KEYCLOAK_ADMIN_USER" \
        --data-urlencode "password=$KEYCLOAK_ADMIN_PASSWORD" \
        | jq -r .access_token
}
kc() {  # kc <METOD> <yol> [gövde]
    local m="$1" p="$2" body="${3:-}"
    if [[ -n "$body" ]]; then
        curl -sS -o /tmp/kc.out -w '%{http_code}' -X "$m" "$AUTH_URL/admin/realms$p" \
            -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "$body"
    else
        curl -sS -o /tmp/kc.out -w '%{http_code}' -X "$m" "$AUTH_URL/admin/realms$p" \
            -H "Authorization: Bearer $TOKEN"
    fi
}

if calisir keycloak; then
    dc pull keycloak caddy
    dc up -d keycloak caddy
    bilgi "Keycloak'ın PUBLIC adresten yanıt vermesi bekleniyor (DNS + TLS + Caddy + Keycloak)…"
    for i in $(seq 1 90); do
        if curl -fsS "$AUTH_URL/realms/master/.well-known/openid-configuration" >/dev/null 2>&1; then break; fi
        [[ $i -eq 90 ]] && dur "$AUTH_URL 180 sn içinde yanıt vermedi. DNS, 80/443 portları ve sertifika alımını kontrol edin."
        sleep 2
    done
    iyi "Keycloak public adresten yanıt veriyor: $AUTH_URL"

    TOKEN="$(kc_token)"
    [[ -n "$TOKEN" && "$TOKEN" != "null" ]] || dur "Keycloak bootstrap yöneticisiyle token alınamadı."

    # ── Realm: yoksa şablondan yarat ────────────────────────────────────────
    if [[ "$(kc GET "/$REALM")" == "200" ]]; then
        iyi "realm '$REALM' zaten var — yakınsanacak"
    else
        bilgi "realm '$REALM' yok, şablondan yaratılıyor"
        jq --arg d "$APP_DOMAIN" '
            walk(if type == "string" then gsub("__APP_DOMAIN__"; $d) else . end)
        ' "$REALM_TEMPLATE" > /tmp/realm.json
        code="$(kc POST "" "$(cat /tmp/realm.json)")"
        [[ "$code" =~ ^20 ]] || dur "realm yaratılamadı (HTTP $code): $(head -c 400 /tmp/kc.out)"
        rm -f /tmp/realm.json
        iyi "realm '$REALM' yaratıldı"
    fi

    # ── Yakınsama 1: unmanaged öznitelik politikası ─────────────────────────
    # ENABLED olsaydı kullanıcı manage-account ile kendine branch_codes ekleyip kapsamını
    # aşabilirdi (#126). Politika BOŞSA doğrulayıcı sapma ÜRETMEZ — burada garanti edilir.
    kc GET "/$REALM/users/profile" >/dev/null
    profil="$(jq '.unmanagedAttributePolicy = "ADMIN_EDIT"' /tmp/kc.out)"
    code="$(kc PUT "/$REALM/users/profile" "$profil")"
    [[ "$code" =~ ^2 ]] && iyi "unmanagedAttributePolicy = ADMIN_EDIT" \
                        || uyar "öznitelik politikası yazılamadı (HTTP $code)"

    # ── Yakınsama 2: roller ─────────────────────────────────────────────────
    # Rol listesinin kaynağı ŞABLONDUR; şablon da testle MesnetRoles.All'a kilitlidir
    # (ProductionRealmTemplateTests). Burada ikinci bir liste TUTULMAZ — tutulsaydı üçüncü
    # bir sapma yüzeyi doğardı.
    kc GET "/$REALM/roles?briefRepresentation=true&max=200" >/dev/null
    mevcut="$(jq -r '.[].name' /tmp/kc.out | sort)"
    eklenen=0
    while read -r rol; do
        [[ -n "$rol" ]] || continue
        if ! grep -qxF "$rol" <<<"$mevcut"; then
            code="$(kc POST "/$REALM/roles" "$(jq -nc --arg n "$rol" '{name:$n}')")"
            [[ "$code" =~ ^2 ]] && eklenen=$((eklenen+1)) || uyar "rol eklenemedi: $rol (HTTP $code)"
        fi
    done < <(jq -r '.roles.realm[].name' "$REALM_TEMPLATE")
    toplam="$(jq -r '.roles.realm | length' "$REALM_TEMPLATE")"
    iyi "roller yakınsandı — $toplam beklenen, $eklenen yeni eklendi"

    # ── Yakınsama 3: mesnet-web adresleri ───────────────────────────────────
    # Şablon localhost taşımaz; ama var olan bir realm'de alan adı değişmiş olabilir.
    kc GET "/$REALM/clients?clientId=mesnet-web" >/dev/null
    web_uuid="$(jq -r '.[0].id // empty' /tmp/kc.out)"
    if [[ -n "$web_uuid" ]]; then
        web="$(jq --arg d "$APP_DOMAIN" '.[0]
                 | .publicClient = true
                 | .redirectUris = ["https://\($d)/*"]
                 | .webOrigins   = ["https://\($d)"]' /tmp/kc.out)"
        code="$(kc PUT "/$REALM/clients/$web_uuid" "$web")"
        [[ "$code" =~ ^2 ]] && iyi "mesnet-web adresleri https://$APP_DOMAIN olarak yazıldı" \
                            || uyar "mesnet-web güncellenemedi (HTTP $code)"
    else
        uyar "mesnet-web client'ı bulunamadı — SPA giriş yapamaz."
    fi

    # ── Client secret: KEYCLOAK üretir, biz okuruz ──────────────────────────
    kc GET "/$REALM/clients?clientId=$API_CLIENT" >/dev/null
    api_uuid="$(jq -r '.[0].id // empty' /tmp/kc.out)"
    [[ -n "$api_uuid" ]] || dur "'$API_CLIENT' client'ı realm'de yok — Admin API hiç çalışmaz."
    kc GET "/$REALM/clients/$api_uuid/client-secret" >/dev/null
    secret="$(jq -r '.value // empty' /tmp/kc.out)"
    [[ -n "$secret" ]] || dur "$API_CLIENT client secret'ı okunamadı."

    # .env'e yaz. Elle yazılan bir değer iki tarafı ayrıştırırdı; belirtisi hata değil,
    # "kullanıcı listesi boş" olurdu.
    if grep -q '^KEYCLOAK_CLIENT_SECRET=' "$ENV_FILE"; then
        tmp="$(mktemp)"; trap 'rm -f "$tmp"' EXIT
        awk -v s="$secret" '/^KEYCLOAK_CLIENT_SECRET=/{print "KEYCLOAK_CLIENT_SECRET=" s; next} {print}' \
            "$ENV_FILE" > "$tmp"
        cat "$tmp" > "$ENV_FILE"      # izinleri korumak için üzerine yaz, mv DEĞİL
    else
        printf 'KEYCLOAK_CLIENT_SECRET=%s\n' "$secret" >> "$ENV_FILE"
    fi
    iyi "client secret Keycloak'tan okunup .env'e yazıldı (ekrana yazılmadı)"
    rm -f /tmp/kc.out
fi

# ═══════════════════════════════════════════════════════════════════════════
faz "4/5  Uygulama"
# ═══════════════════════════════════════════════════════════════════════════
if calisir uygulama; then
    set -a; # shellcheck disable=SC1090
    source "$ENV_FILE"; set +a          # yeni secret'ı al
    [[ -n "${KEYCLOAK_CLIENT_SECRET:-}" ]] || dur "KEYCLOAK_CLIENT_SECRET boş — önce 'keycloak' fazını koşturun."

    dc pull
    dc up -d
    bilgi "API'nin sağlıklı olması bekleniyor…"
    for i in $(seq 1 90); do
        durum="$(dc ps --format json 2>/dev/null | jq -r 'select(.Service=="api") | .Health' 2>/dev/null | head -1)"
        [[ "$durum" == "healthy" ]] && break
        if [[ $i -eq 90 ]]; then
            oldu "API 180 sn içinde sağlıklı olmadı. Son kayıtlar:"
            dc logs --tail 40 api >&2 || true
            exit 1
        fi
        sleep 2
    done
    iyi "API sağlıklı"
fi

# ═══════════════════════════════════════════════════════════════════════════
faz "5/5  İlk yönetici"
# ═══════════════════════════════════════════════════════════════════════════
if calisir yonetici && (( ! SKIP_BOOTSTRAP )); then
    # SystemAdmin, kurum SINIRININ ÜSTÜNDE çalışan tek roldür: yeni okul açmak
    # (platform:tenant:manage) ve o okulun ilk kullanıcısını bağlamak yalnız onda vardır.
    # Okul VERİSİ ona kapalıdır — institution:view/manage verilmez.
    TOKEN="$(kc_token)"
    read -r -p "   İlk yönetici kullanıcı adı [mesnetadmin]: " yonetici
    yonetici="${yonetici:-mesnetadmin}"
    read -r -p "   E-posta: " eposta
    read -rsp "   Parola (ekrana yazılmaz): " parola; echo
    read -rsp "   Parola (tekrar): " parola2; echo
    [[ "$parola" == "$parola2" ]] || dur "Parolalar eşleşmedi."
    [[ ${#parola} -ge 12 ]] || dur "Parola en az 12 karakter olmalı."

    kc GET "/$REALM/users?username=$yonetici&exact=true" >/dev/null
    uid="$(jq -r '.[0].id // empty' /tmp/kc.out)"
    if [[ -z "$uid" ]]; then
        govde="$(jq -nc --arg u "$yonetici" --arg e "$eposta" --arg p "$parola" \
            '{username:$u, email:$e, enabled:true, emailVerified:true,
              credentials:[{type:"password", value:$p, temporary:false}]}')"
        code="$(kc POST "/$REALM/users" "$govde")"
        [[ "$code" =~ ^2 ]] || dur "kullanıcı yaratılamadı (HTTP $code): $(head -c 300 /tmp/kc.out)"
        kc GET "/$REALM/users?username=$yonetici&exact=true" >/dev/null
        uid="$(jq -r '.[0].id' /tmp/kc.out)"
        iyi "kullanıcı '$yonetici' yaratıldı"
    else
        iyi "kullanıcı '$yonetici' zaten var — rol ataması yakınsanacak"
    fi
    unset parola parola2

    kc GET "/$REALM/roles/SystemAdmin" >/dev/null
    rol="$(jq -c '{id, name}' /tmp/kc.out)"
    code="$(kc POST "/$REALM/users/$uid/role-mappings/realm" "[$rol]")"
    [[ "$code" =~ ^2 ]] && iyi "SystemAdmin rolü atandı" || uyar "rol atanamadı (HTTP $code)"
    rm -f /tmp/kc.out
fi

# ═══════════════════════════════════════════════════════════════════════════
faz "Kurulum tamam — SIRADAKİ ADIMLAR ELLE"
# ═══════════════════════════════════════════════════════════════════════════
cat <<SONRAKI

   Sistem ayakta ama HENÜZ KULLANILABİLİR DEĞİL. Üç adım kaldı ve sırası önemlidir.

   1) Kullanıcı kaydını Keycloak'tan senkronize edin.
      UserAccount kaydı OTORİTERDİR: kayıt yoksa token'daki roller izin ÜRETMEZ ve uçlar
      403 döner. İzin önbelleği nedeniyle etkisi 5 dakikaya kadar gecikebilir.

        POST https://${APP_DOMAIN}/api/security/users/sync

   2) İlk okulu ve müdürünü açın ('$yonetici' hesabıyla giriş yapın):

        https://${APP_DOMAIN}

   3) Dağıtım ön koşullarını koşturun — ATLANIRSA sistem hata vermez, özellik sessizce çalışmaz:

        export MESNET_API_URL=https://${APP_DOMAIN}
        export MESNET_KEYCLOAK_TOKEN_URL=${AUTH_URL}/realms/${REALM}/protocol/openid-connect/token
        export MESNET_OPERATOR_USER=$yonetici
        $REPO_ROOT/scripts/deploy-prereqs.sh --dry-run    # önce planı görün
        $REPO_ROOT/scripts/deploy-prereqs.sh

   Ayrıntı ve sıra gerekçeleri:
     src/Docs/docs/infrastructure/dagitim-on-kosullari.md

SONRAKI
