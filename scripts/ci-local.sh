#!/usr/bin/env bash
#
# CI işlerini YERELDE koşar — .github/workflows/ci.yml'in aynası.
#
#   ./scripts/ci-local.sh backend|frontend|docs|integration|all
#
# NEDEN VAR: CI dakika kotası dolduğunda (16.08.2026'dan beri) her koşu 0 adımda düşüyor ve
# hiçbir kapı çalışmıyor. Bu script o kapıları yerelde açar.
#
# `act` KULLANILMADI: podman soketi ek kurulum ister, arm64'te runner imajları büyük,
# `services:` blokları ve `actions/cache` tam eşlenmiyor, entegrasyon işi runner'ın İÇİNDE
# `docker compose` koşuyor (iç içe konteyner).
#
# ⚠ SÜRDÜRÜLEBİLİRLİK: bu script ci.yml'den SESSİZCE ıraksayabilir. ci.yml'deki adımlar
# değişirse burası da güncellenmelidir; `check` alt komutu kaba bir sapma kontrolü yapar.
#
# YEREL UYARLAMALAR (CI'dan farkı ve nedeni):
#   • podman compose        — depo Podman kullanıyor (CLAUDE.md), Docker değil
#   • kaydırılmış portlar   — geliştirme yığını 5432/5672/9000'i tutuyor; iki yığın aynı anda
#                             ayakta durabilmeli. Portlar compose'da parametrik, CI'da varsayılan
#   • API 5271             — 5270'te geliştirme API'si çalışıyor olabilir
#   • appsettings'e DOKUNULMAZ — CI onu sample'dan üretir (dosya git'te yok); yerelde SİZİN
#                             dosyanız var ve ezilmemeli. Ortam değişkenleri JSON'u zaten ezer
#   • docusaurus doğrudan   — `pnpm build` ön-kontrolü core-js build-script kapısına takılıyor
#   • rabbitmq `podman run` — compose ile açılmıyor (.erlang.cookie/eacces). Denenen çözümler
#                             ve ÇÖZÜLEMEYEN kalan sorun: compose.ci.local.yml
#
# ⚠ ENTEGRASYON İŞİ ŞU AN YEREL MAKİNEDE TAM KOŞMUYOR — bkz. compose.ci.local.yml.
#   Diğer üç iş (backend/frontend/docs) sorunsuz koşuyor ve asıl değeri onlar veriyor.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."
REPO_ROOT="$PWD"

COMPOSE_FILE=".github/compose.ci.yml"
# Yerel uyarlamalar ayrı dosyada — CI dosyası olduğu gibi kalsın (bkz. compose.ci.local.yml).
COMPOSE_LOCAL=".github/compose.ci.local.yml"
PROJECT="mesnet-ci"
RABBIT_NAME="mesnet-ci-rabbit"

# Geliştirme yığınıyla çakışmayan portlar
export CI_PG_PORT="${CI_PG_PORT:-5433}"
export CI_RABBIT_PORT="${CI_RABBIT_PORT:-5673}"
export CI_KEYCLOAK_PORT="${CI_KEYCLOAK_PORT:-8081}"
export CI_MINIO_PORT="${CI_MINIO_PORT:-9010}"
API_PORT="${CI_API_PORT:-5271}"

# Docs işi diyagramları Kroki ile render ediyor. Geliştirme yığınında zaten bir Kroki var;
# varsa onu kullan, yoksa render atlanır (derlemeyi DÜŞÜRMEZ — ci.yml'de de öyle).
KROKI_SERVER="${KROKI_SERVER:-http://localhost:9222}"

log()  { printf '\n\033[1;36m▶ %s\033[0m\n' "$*"; }
ok()   { printf '\033[1;32m✔ %s\033[0m\n' "$*"; }
fail() { printf '\033[1;31m✘ %s\033[0m\n' "$*" >&2; exit 1; }

compose() { podman compose -p "$PROJECT" -f "$COMPOSE_FILE" -f "$COMPOSE_LOCAL" "$@"; }

# ─────────────────────────────────────────────────────────────────────────────
# İş 1 — Backend: derleme + birim testler (ci.yml: "Backend — derleme")
# ─────────────────────────────────────────────────────────────────────────────
job_backend() {
  log "Backend — çözümü derle (Release)"
  dotnet build MESNET.slnx -c Release

  log "Backend — birim testler (Release)"
  # Glob ile taranır, liste sabitlenmez — ci.yml ile aynı gerekçe: yeni modülün testleri
  # eklenmeyi unutulduğu için atlanmasın.
  shopt -s nullglob
  local projects=(tests/*.UnitTests/*.UnitTests.csproj)
  [ ${#projects[@]} -gt 0 ] || fail "Hiç birim test projesi bulunamadı — glob bozulmuş olabilir."

  for project in "${projects[@]}"; do
    printf '  • %s\n' "$project"
    dotnet test "$project" -c Release --no-build --logger 'console;verbosity=quiet'
  done
  ok "Backend"
}

# ─────────────────────────────────────────────────────────────────────────────
# İş 2 — Frontend: lint + vitest + type-check & build
# ─────────────────────────────────────────────────────────────────────────────
job_frontend() {
  cd "$REPO_ROOT/src/WebUI"

  log "Frontend — bağımlılıklar"
  pnpm install --frozen-lockfile

  log "Frontend — lint"
  pnpm lint

  log "Frontend — vitest"
  pnpm test:run

  log "Frontend — type-check & build"
  pnpm build

  cd "$REPO_ROOT"
  ok "Frontend"
}

# ─────────────────────────────────────────────────────────────────────────────
# İş 3 — Dokümanlar: Docusaurus derleme
#
# Değeri docusaurus.config.ts'deki `throw` ayarlarından gelir: onBrokenLinks,
# onBrokenAnchors, onBrokenMarkdownLinks. Diyagram render hatası derlemeyi DÜŞÜRMEZ.
# ─────────────────────────────────────────────────────────────────────────────
job_docs() {
  cd "$REPO_ROOT/src/Docs"

  log "Dokümanlar — bağımlılıklar"
  pnpm install --frozen-lockfile --ignore-scripts

  if curl -sf --max-time 3 "$KROKI_SERVER/health" >/dev/null 2>&1; then
    printf '  Kroki: %s\n' "$KROKI_SERVER"
  else
    printf '  \033[1;33m! Kroki erişilemez (%s) — diyagramlar render edilmeyecek\033[0m\n' "$KROKI_SERVER"
  fi

  log "Dokümanlar — derleme"
  # `pnpm build` ön-kontrolü core-js build-script onayına takılıyor; ikili doğrudan çağrılır.
  KROKI_SERVER="$KROKI_SERVER" node_modules/.bin/docusaurus build

  cd "$REPO_ROOT"
  ok "Dokümanlar"
}

# ─────────────────────────────────────────────────────────────────────────────
# İş 4 — Entegrasyon: yığın + API + kara-kutu testler + seeder
# ─────────────────────────────────────────────────────────────────────────────
API_PID=""
API_LOG="$(mktemp -t mesnet-ci-api).log"

integration_cleanup() {
  local code=$?

  if [ $code -ne 0 ] && [ -f "$API_LOG" ]; then
    printf '\n───────── API log (son 200 satır) ─────────\n'
    tail -200 "$API_LOG" || true
    printf '\n───────── Konteyner logları ─────────\n'
    compose logs --tail 100 || true
    podman logs --tail 40 "${RABBIT_NAME}" 2>&1 | tail -40 || true
  fi

  if [ -n "$API_PID" ] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi

  # Yığın her hâlükârda kapatılır — ci.yml'deki `if: always()` karşılığı.
  podman rm -f "${RABBIT_NAME}" >/dev/null 2>&1 || true
  compose down -v >/dev/null 2>&1 || true
}

wait_for() {
  local what="$1" url="$2" expect="$3" tries="${4:-60}"
  for _ in $(seq 1 "$tries"); do
    if [ "$(curl -s -o /dev/null -w '%{http_code}' --max-time 3 "$url")" = "$expect" ]; then
      ok "$what hazır"
      return 0
    fi
    sleep 5
  done
  fail "$what ${tries}x5 sn içinde hazır olmadı: $url"
}

job_integration() {
  command -v podman >/dev/null || fail "podman bulunamadı."

  trap integration_cleanup EXIT

  log "Entegrasyon — bağımlılık yığını (pg:$CI_PG_PORT kc:$CI_KEYCLOAK_PORT minio:$CI_MINIO_PORT)"
  # ÖNCE ZORLA TEMİZLE: `compose down` podman'da sessizce başarısız olabiliyor ve `up -d`
  # var olan konteyneri YENİ YAPILANDIRMAYI UYGULAMADAN yeniden kullanıyor. Ölçüldü: tmpfs
  # ayarı değiştirildiği hâlde bir saatlik eski konteyner ayakta kalmıştı.
  podman ps -aq --filter "name=${PROJECT}" | xargs -r podman rm -f >/dev/null 2>&1 || true
  # rabbitmq compose DIŞINDA başlatılır — gerekçe compose.ci.local.yml içinde.
  compose up -d --force-recreate postgres keycloak minio

  log "Entegrasyon — RabbitMQ (compose dışı, :$CI_RABBIT_PORT)"
  podman rm -f "${RABBIT_NAME}" >/dev/null 2>&1 || true
  podman run -d --name "${RABBIT_NAME}" \
    --tmpfs /var/lib/rabbitmq:rw,mode=0777 \
    -e RABBITMQ_DEFAULT_USER=mesnet \
    -e RABBITMQ_DEFAULT_PASS=mesnet_dev \
    -p "${CI_RABBIT_PORT}:5672" \
    rabbitmq:4-alpine >/dev/null

  # SESSİZ DEVAM YOK: ilk sürüm döngü tükendiğinde devam ediyor ve hata 5 dakika sonra
  # "API hazır olmadı" olarak görünüyordu — asıl sebep gizleniyordu.
  local rabbit_ready=0
  for _ in $(seq 1 30); do
    if podman exec "${RABBIT_NAME}" rabbitmq-diagnostics -q ping >/dev/null 2>&1; then
      rabbit_ready=1
      ok "RabbitMQ hazır"
      break
    fi
    sleep 3
  done

  if [ "$rabbit_ready" -ne 1 ]; then
    printf '\n───────── RabbitMQ log ─────────\n'
    podman logs --tail 60 "${RABBIT_NAME}" 2>&1 | tail -60 || echo "(konteyner hiç oluşmadı)"
    fail "RabbitMQ 90 sn içinde hazır olmadı."
  fi

  wait_for "Keycloak mesnet realm" \
    "http://localhost:${CI_KEYCLOAK_PORT}/realms/mesnet/.well-known/openid-configuration" 200

  log "Entegrasyon — API'yi derle (Release)"
  dotnet build src/MESNET.Presentation/MESNET.Presentation.csproj -c Release

  log "Entegrasyon — API'yi başlat (:$API_PORT)"
  # appsettings.Development.json'a DOKUNULMAZ. Ortam değişkenleri JSON'u ezer, yani sizin
  # geliştirme yapılandırmanız yerinde kalırken API bu yığına bağlanır.
  # `env` KULLANILIYOR, VAR=val öneki DEĞİL: Keycloak anahtarı tire içeriyor
  # (`auth-server-url`) ve bash tireli adı değişken ataması olarak kabul etmiyor —
  # "No such file or directory" ile düşüyor.
  env \
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS="http://127.0.0.1:${API_PORT}" \
    ConnectionStrings__mesnet="Host=localhost;Port=${CI_PG_PORT};Database=mesnet;Username=mesnet;Password=mesnet_dev" \
    RabbitMQ__HostName=localhost \
    RabbitMQ__Port="${CI_RABBIT_PORT}" \
    RabbitMQ__UserName=mesnet \
    RabbitMQ__Password=mesnet_dev \
    "Keycloak__auth-server-url=http://localhost:${CI_KEYCLOAK_PORT}/" \
    Keycloak__credentials__secret=dev-secret \
    MinioStorage__Endpoint="localhost:${CI_MINIO_PORT}" \
    MinioStorage__AccessKey=minioadmin \
    MinioStorage__SecretKey=minioadmin \
    dotnet run --project src/MESNET.Presentation \
      --no-launch-profile --no-build -c Release > "$API_LOG" 2>&1 &
  API_PID=$!

  wait_for "API" "http://127.0.0.1:${API_PORT}/health" 200

  # Realm doğrulaması Development'ta sapmada AÇILIŞI DURDURUR (#195) — API ayağa kalktıysa
  # sapma yoktur. Ama "doğrulandı" ile "sessiz geçildi" aynı görünür; satırlar basılır.
  log "Entegrasyon — realm doğrulama çıktısı"
  grep -iE "Realm doğrulan|Realm ayarlar|Realm doğrulaması atlandı|SAPMIŞ" "$API_LOG" \
    || printf '\033[1;33m! Realm doğrulama satırı bulunamadı — kontrol koşmamış olabilir (#195)\033[0m\n'

  log "Entegrasyon — kara-kutu API testleri"
  API_BASE_URL="http://127.0.0.1:${API_PORT}" \
  KEYCLOAK_TOKEN_URL="http://localhost:${CI_KEYCLOAK_PORT}/realms/mesnet/protocol/openid-connect/token" \
    dotnet test tests/MESNET.Api.Tests/MESNET.Api.Tests.csproj -c Release \
      --logger 'console;verbosity=normal'

  # Seeder BOŞ veritabanında koşar — asıl idempotency sınavı bu (#80). Testlerden SONRA
  # çalışır ki seed edilen veri test beklentilerini bozmasın.
  log "Entegrasyon — seeder (boş veritabanında)"
  Seeder__ApiBaseUrl="http://127.0.0.1:${API_PORT}" \
  Seeder__KeycloakTokenUrl="http://localhost:${CI_KEYCLOAK_PORT}/realms/mesnet/protocol/openid-connect/token" \
    dotnet run --project src/MESNET.Seeder -c Release

  ok "Entegrasyon"
}

# ─────────────────────────────────────────────────────────────────────────────
# Sapma kontrolü — bu script ci.yml'den sessizce ıraksamasın
# ─────────────────────────────────────────────────────────────────────────────
job_check() {
  log "Sapma kontrolü — ci.yml'deki komutlar burada da var mı"

  python3 - "$0" <<'PY'
import re, sys, pathlib

script = pathlib.Path(sys.argv[1]).read_text(encoding="utf-8")
workflow = pathlib.Path(".github/workflows/ci.yml").read_text(encoding="utf-8")

def normalize(text):
    """Satır sarmasını ve boşluk farklarını sil — karşılaştırma anlamlı olsun."""
    return re.sub(r"\s+", " ", text.replace("\\\n", " "))

# YORUM SATIRLARI ATLANIR: ilk sürüm bunu yapmıyordu ve yorumdaki
# "dotnet test koşuyor." cümlesini komut sanıp yanlış alarm veriyordu.
lines = [l for l in workflow.splitlines() if not l.lstrip().startswith("#")]
haystack = normalize(script)

pattern = re.compile(r"(?:dotnet (?:build|test|run)|pnpm (?:lint|build|test:run|install))[^\n]*")
commands = {normalize(m.group()).strip() for l in lines for m in pattern.finditer(l)}

missing = []
for cmd in sorted(commands):
    # Komutun ilk üç sözcüğü scriptte geçiyorsa eşleşmiş sayılır: bayrak sırası ve
    # yerel uyarlamalar (port, --no-build) bilerek farklı olabilir.
    head = " ".join(cmd.split()[:3])
    if head not in haystack:
        missing.append(cmd)

if missing:
    print("\033[1;33m! ci.yml şu komutları içeriyor ama script içinde bulunamadı:\033[0m")
    for m in missing:
        print(f"  - {m}")
    sys.exit(1)

print(f"\033[1;32m✔ ci.yml'deki {len(commands)} komut şeklinin hepsi script içinde\033[0m")
PY
}

case "${1:-all}" in
  backend)     job_backend ;;
  frontend)    job_frontend ;;
  docs)        job_docs ;;
  integration) job_integration ;;
  check)       job_check ;;
  all)         job_backend; job_frontend; job_docs; job_integration ;;
  *)           fail "Kullanım: $0 backend|frontend|docs|integration|check|all" ;;
esac
