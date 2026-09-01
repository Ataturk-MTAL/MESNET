#!/usr/bin/env bash
#
# MESNET — dağıtım ön koşullarını sırayla koşturur.
#
# NEDEN AYRI BİR BETİK, NEDEN AÇILIŞTA DEĞİL
# ------------------------------------------
# Açılıştan koşturmak bu depoda mümkün değildir, üç ölçülmüş nedenle:
#   1. Wolverine `UseWolverine` ile host'tan SONRA başlar (Program.cs). Açılıştan yapılan her
#      yayın `WolverineHasNotStartedException` fırlatır — resync uçlarının hepsi olay yayınlar.
#   2. İki uç idempotent DEĞİLDİR (#290, #291); her yeniden başlatmada sayacı biraz daha bozardı.
#   3. `client_credentials` servis hesabının realm rolü yoktur; bağlam değiştirme token'da `sid`
#      ister ve servis hesabı token'ında `sid` bulunmaz.
#
# Açılış ÖLÇER (`DeploymentPrerequisiteVerificationHostedService`), operatör KOŞTURUR (bu betik).
#
# KİMLİK
# ------
# Adlandırılmış bir OPERATÖR hesabı kullanılır — kalıcı bir `DeploymentOperator` servis hesabı
# DEĞİL. Gerekçe: servis hesabı yılda beş kez kullanılmak için 365 gün boyunca bütün okulların
# verisine yazma yetkisi taşıyan kalıcı bir anahtar olurdu. Parola bu betikte SAKLANMAZ; çalışma
# anında ortam değişkeninden ya da terminalden alınır ve süreçle birlikte ölür. Denetim kaydı
# gerçek bir kişinin `sub`'unu taşır.
#
# VARSAYILAN YOKTUR. Ne URL ne kimlik bilgisi tahmin edilir: yanlış hedefe sessizce koşan bir
# dağıtım betiği, koşmayan betikten kötüdür. Geliştirme için `--dev` bayrağı vardır.
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STATE_FILE="${MESNET_DEPLOY_STATE:-$SCRIPT_DIR/.deploy-prereqs-state}"

DRY_RUN=0
INCLUDE_BROKEN=0
ALLOW_ONCE=0
ONLY=""
DEV=0

usage() {
    cat <<'USAGE'
Kullanım: deploy-prereqs.sh [seçenekler]

  --dry-run           Hiçbir uç çağrılmaz; plan ve her fazın durumu yazılır
  --dev               URL'leri yerel geliştirme değerlerine kurar (kimlik bilgisi YİNE gerekir)
  --only <faz-id>     Yalnız o fazı koşturur (sıra denetimi çağıranın sorumluluğundadır)
  --allow-once        İdempotent OLMAYAN, "tam bir kez" işaretli fazları koşturmaya izin verir
  --include-broken    "broken" işaretli fazları da koşturur — VERİ BOZAR.
                      Bugün bu sınıfta HİÇBİR faz yok (#290, #291, #292 kapandı); bayrak,
                      bir sonraki bozuk uç çıktığında yeniden yazılmasın diye duruyor
  -h, --help          Bu metin

Ortam değişkenleri (varsayılanı olmayanlar zorunludur):
  MESNET_API_URL                 ör. https://mesnet.example.gov.tr
  MESNET_KEYCLOAK_TOKEN_URL      ör. https://kc.example.gov.tr/realms/mesnet/protocol/openid-connect/token
  MESNET_CLIENT_ID               varsayılan: mesnet-api
  MESNET_OPERATOR_USER           platform:tenant:manage taşıyan gerçek kullanıcı
  MESNET_OPERATOR_PASSWORD       verilmezse terminalden sorulur (ekrana yazılmaz)
  MESNET_OPERATOR_TOKEN          parola yerine hazır erişim token'ı
  MESNET_DEPLOY_STATE            "tam bir kez" damgalarının tutulduğu dosya
USAGE
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --dry-run) DRY_RUN=1; shift ;;
        --dev) DEV=1; shift ;;
        --only) ONLY="${2:?--only bir faz kimliği ister}"; shift 2 ;;
        --allow-once) ALLOW_ONCE=1; shift ;;
        --include-broken) INCLUDE_BROKEN=1; shift ;;
        -h|--help) usage; exit 0 ;;
        *) echo "Bilinmeyen seçenek: $1" >&2; usage >&2; exit 2 ;;
    esac
done

for tool in curl jq; do
    command -v "$tool" >/dev/null 2>&1 || { echo "HATA: '$tool' bulunamadı." >&2; exit 2; }
done

if [[ $DEV -eq 1 ]]; then
    : "${MESNET_API_URL:=http://localhost:5226}"
    : "${MESNET_KEYCLOAK_TOKEN_URL:=http://localhost:8080/realms/mesnet/protocol/openid-connect/token}"
fi
: "${MESNET_CLIENT_ID:=mesnet-api}"

: "${MESNET_API_URL:?MESNET_API_URL zorunludur — hedef tahmin edilmez}"
: "${MESNET_KEYCLOAK_TOKEN_URL:?MESNET_KEYCLOAK_TOKEN_URL zorunludur}"

API="${MESNET_API_URL%/}"

# ──────────────────────────────────────────────────────────────────────────────
# Faz tablosu — SIRA ANLAMLIDIR.
#
#   id | sınıf | uç | doğrulama (jq ifadesi; boşsa faz doğrulanmaz ve öyle YAZILIR)
#
# sınıf:
#   safe   — idempotent, serbestçe yeniden koşturulur
#   once   — gerekli ama idempotent DEĞİL; tam bir kez koşturulur (--allow-once + damga)
#   broken — bilinen hatalı; varsayılan olarak ATLANIR (--include-broken)
#
# Sıra gerekçeleri (src/Docs/docs/infrastructure/dagitim-on-kosullari.md):
#   - Ağaç önce kurulur: kapsam yol önekinden türer, yolu olmayan okul hiçbir alt ağaçta yoktur.
#   - Kiracı anahtarı backfill'i kullanıcı işlerinden ÖNCE gelir; sonra koşarsa kullanıcılar
#     kapsamsız kalır ve boş liste görürler.
#   - Yetki backfill'i koordinasyon görünümlerinden ÖNCE; ters sırada yerleştirme tümden durur.
#   - Tekilleştirme bağlamadan ÖNCE; kopyalar dururken bağlamak 24 kardeşten rastgele birine
#     bağlamak demektir.
# ──────────────────────────────────────────────────────────────────────────────
PHASES=(
  "hierarchy|safe|POST|/api/institutions/rebuild-hierarchy|.data.skippedNoProvince == 0|Kurum ağacı: il/ilçe düğümleri ve Path"
  "staff-scope|safe|POST|/api/institutions/staff/resync-branch-codes||Personel kaydından kullanıcıya kurum + alan kapsamı"
  "users-sync|safe|POST|/api/security/users/sync||Keycloak kullanıcılarını yerel kayda çeker"
  "users-replay|safe|POST|/api/security/users/replay|.data.replayed > 0|Yönetici bağı görünümü (müdürlük panosu)"
  "students|safe|POST|/api/students/resync-projections|.data.studentCount > 0|Öğrenci kapsam otoritesi + veli bağı ön koşulu; şube sayacını da mutlak tazeler (#290 onarıldı)"
  "businesses|safe|POST|/api/businesses/resync-projections||İşletme görünümleri"
  "branch-auth|safe|POST|/api/placements/backfill-branch-authorizations||İşletme alan yetkileri — koordinasyondan ÖNCE"
  "placements|safe|POST|/api/placements/resync-projections|.data.published > 0|Yerleştirme görünümleri (Payment, Coordination, Reporting) — #291 onarıldı"
  "coord-teachers|safe|POST|/api/coordination/teachers/resync-views||Koordinasyon öğretmen görünümleri"
  "coord-visits|safe|POST|/api/coordination/weekly-visits/resync||Haftalık ziyaret olayları"
  "attendance|safe|POST|/api/attendance/resync-snapshots||Devamsızlık anlık görüntüleri (Payment + Reporting)"
  "sagas|safe|POST|/api/internships/resync-sagas|.data.tenantsProcessed > 0|Kopya staj saga'larını birleştirir — TÜM kiracıları dolaşır (#292 onarıldı)"
  "internship-links|safe|POST|/api/contracts/resync-internship-links|.data.tenantsProcessed > 0|Aktif sözleşmeleri saga'ya bağlar — sagas'tan SONRA gelmelidir (#292 onarıldı)"
  "display-names|safe|POST|/api/security/users/resync-display-names||Kullanıcı görünen adları (kozmetik, en son)"
)

# ──────────────────────────────────────────────────────────────────────────────
# Token — parola ne loglanır ne de dosyaya yazılır.
# ──────────────────────────────────────────────────────────────────────────────
acquire_token() {
    if [[ -n "${MESNET_OPERATOR_TOKEN:-}" ]]; then
        printf '%s' "$MESNET_OPERATOR_TOKEN"
        return
    fi

    : "${MESNET_OPERATOR_USER:?MESNET_OPERATOR_USER zorunludur — adlandırılmış operatör hesabı}"

    local password="${MESNET_OPERATOR_PASSWORD:-}"
    if [[ -z "$password" ]]; then
        [[ -t 0 ]] || { echo "HATA: parola yok ve terminal yok. MESNET_OPERATOR_PASSWORD verin." >&2; exit 2; }
        read -r -s -p "Operatör parolası ($MESNET_OPERATOR_USER): " password
        echo >&2
    fi

    local response
    response=$(curl -sS -X POST "$MESNET_KEYCLOAK_TOKEN_URL" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        --data-urlencode "grant_type=password" \
        --data-urlencode "client_id=$MESNET_CLIENT_ID" \
        --data-urlencode "username=$MESNET_OPERATOR_USER" \
        --data-urlencode "password=$password") || {
            echo "HATA: token uç noktasına ulaşılamadı." >&2; exit 1; }

    unset password

    local token
    token=$(printf '%s' "$response" | jq -r '.access_token // empty')
    if [[ -z "$token" ]]; then
        # Yanıt gövdesi parola İÇERMEZ; hata kodunu göstermek teşhis için gerekli.
        echo "HATA: token alınamadı: $(printf '%s' "$response" | jq -c '{error, error_description}' 2>/dev/null || echo "$response")" >&2
        exit 1
    fi
    printf '%s' "$token"
}

stamped() { [[ -f "$STATE_FILE" ]] && grep -qxF "$1" "$STATE_FILE"; }
stamp()   { printf '%s\n' "$1" >> "$STATE_FILE"; }

TOTAL=0; RAN=0; SKIPPED=0; FAILED=0; SUSPECT=0
declare -a REPORT=()

run_phase() {
    local id="$1" class="$2" method="$3" path="$4" verify="$5" desc="$6"

    if [[ -n "$ONLY" && "$ONLY" != "$id" ]]; then return 0; fi
    TOTAL=$((TOTAL + 1))

    if [[ "$class" == "broken" && $INCLUDE_BROKEN -eq 0 ]]; then
        SKIPPED=$((SKIPPED + 1))
        REPORT+=("ATLANDI  $id — BOZUK: $desc")
        printf '  [atlandı] %-16s BOZUK — %s\n' "$id" "$desc"
        return 0
    fi

    if [[ "$class" == "once" ]]; then
        if stamped "$id"; then
            SKIPPED=$((SKIPPED + 1))
            REPORT+=("ATLANDI  $id — daha önce koşturulmuş (damga: $STATE_FILE)")
            printf '  [atlandı] %-16s tam-bir-kez, damgası var\n' "$id"
            return 0
        fi
        if [[ $ALLOW_ONCE -eq 0 ]]; then
            SKIPPED=$((SKIPPED + 1))
            REPORT+=("ATLANDI  $id — idempotent değil, --allow-once ister: $desc")
            printf '  [atlandı] %-16s idempotent DEĞİL (--allow-once) — %s\n' "$id" "$desc"
            return 0
        fi
    fi

    if [[ $DRY_RUN -eq 1 ]]; then
        printf '  [plan]    %-16s %s %s\n' "$id" "$method" "$path"
        return 0
    fi

    local body code
    body=$(curl -sS -o /tmp/mesnet-deploy-body.$$ -w '%{http_code}' \
        -X "$method" "$API$path" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Length: 0") || code="000"
    code="${body:-000}"
    body=$(cat /tmp/mesnet-deploy-body.$$ 2>/dev/null || echo '')
    rm -f /tmp/mesnet-deploy-body.$$

    if [[ "$code" != "200" && "$code" != "202" && "$code" != "204" ]]; then
        FAILED=$((FAILED + 1))
        REPORT+=("HATA     $id — HTTP $code: $(printf '%s' "$body" | head -c 300)")
        printf '  [HATA]    %-16s HTTP %s\n' "$id" "$code"
        return 0
    fi

    RAN=$((RAN + 1))

    if [[ -z "$verify" ]]; then
        # Doğrulanamayan faz "başarılı" DİYE yazılmaz. 200 dönmek, işin yapıldığını kanıtlamaz;
        # #292'de tam olarak bu oldu: uç 200 döndü ve sıfır satır işledi.
        REPORT+=("KOŞTU    $id — DOĞRULANMADI (uç 200 döndü; etki ölçülmedi)")
        printf '  [koştu]   %-16s HTTP %s — doğrulanmadı\n' "$id" "$code"
        return 0
    fi

    if printf '%s' "$body" | jq -e "$verify" >/dev/null 2>&1; then
        REPORT+=("TAMAM    $id")
        printf '  [tamam]   %-16s HTTP %s, doğrulandı\n' "$id" "$code"
        [[ "$class" == "once" ]] && stamp "$id"
    else
        SUSPECT=$((SUSPECT + 1))
        REPORT+=("ŞÜPHELİ  $id — uç 200 döndü ama doğrulama tutmadı: $(printf '%s' "$body" | jq -c '.data // .' 2>/dev/null | head -c 300)")
        printf '  [şüpheli] %-16s HTTP %s ama doğrulama TUTMADI\n' "$id" "$code"
    fi
}

echo "=== MESNET dağıtım ön koşulları ==="
echo "  Hedef  : $API"
echo "  Damga  : $STATE_FILE"
[[ $DRY_RUN -eq 1 ]] && echo "  Mod    : KURU KOŞU (hiçbir uç çağrılmaz)"
echo ""

if [[ $DRY_RUN -eq 0 ]]; then
    TOKEN="$(acquire_token)"
else
    TOKEN=""
fi

for row in "${PHASES[@]}"; do
    IFS='|' read -r id class method path verify desc <<< "$row"
    run_phase "$id" "$class" "$method" "$path" "$verify" "$desc"
done

echo ""
echo "=== Özet ==="
# Boş dizi + `set -u` = "unbound variable". Hiçbir faz atlanmadığında (yani her şey yolunda
# gittiğinde) dizi boş kalır; koruma olmadan betik tam da başarı durumunda çökerdi.
if [[ ${#REPORT[@]} -eq 0 ]]; then
    echo "  (rapor satırı yok)"
else
    for line in "${REPORT[@]}"; do echo "  $line"; done
fi
echo ""
printf '  %d faz: %d koştu, %d atlandı, %d şüpheli, %d hata\n' \
    "$TOTAL" "$RAN" "$SKIPPED" "$SUSPECT" "$FAILED"
echo ""
echo "  Olaylar ASENKRON işlenir: 200 dönmek yalnız YAYINLANDI demektir. Panoyu kontrol"
echo "  etmeden önce kuyrukların boşalmasını bekleyin."
echo "  Sıra ve gerekçe: src/Docs/docs/infrastructure/dagitim-on-kosullari.md"

if [[ $FAILED -gt 0 || $SUSPECT -gt 0 ]]; then
    exit 1
fi
