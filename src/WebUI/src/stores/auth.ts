import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type Keycloak from 'keycloak-js'
import api from 'boot/axios'
import { classifyAuthFailure, decodeTokenExp } from 'src/utils/authFailure'

/**
 * `loadPermissions` sonucu (#136). Yönlendirme kararı çağırana bırakılır: yeniden giriş
 * tek huniden geçmeli, yoksa aynı anda tetiklenen `login()` ve `logout()` birbirini ezer.
 */
export type LoadPermissionsOutcome = 'ok' | 'reauth' | 'give-up'

// Keycloak token payload
interface KeycloakTokenParsed {
  sub: string
  preferred_username: string
  email: string
  given_name?: string
  family_name?: string
  realm_access?: { roles: string[] }
  institution_id?: string
  /** Kullanıcının sorumlu olduğu alan (branş) kodları (#126) — multivalued claim */
  branch_codes?: string[] | string
}

/**
 * Kurum genelinde tüm alanların koordinasyon verisine yazma muafiyeti (#126).
 * Backend `Permissions.Institution.AllBranches` ile birebir aynı olmalıdır.
 *
 * Adı bilerek `department:` ile başlamaz: alan şefi `department:*` wildcard'ını taşır,
 * o önekte tanımlansaydı muafiyet alan şefine de geçer ve kapsam kontrolü hiç çalışmazdı.
 */
export const ALL_BRANCHES_PERMISSION = 'institution:distribution:all-branches'

export interface AuthUser {
  id: string
  username: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  roles: string[]
  institutionId: string | null
  /** @deprecated Tek alan varsayımı — yerine `branchCodes` kullanın (#126). */
  branchCode: string | null
  /** Kullanıcının sorumlu olduğu alan kodları; bir kişi birden çok alandan sorumlu olabilir (#126). */
  branchCodes: string[]
}

/**
 * `branch_codes` claim'i mapper yapılandırmasına göre dizi ya da virgüllü metin gelebilir;
 * her iki biçimi de tek tipe indirger (#126).
 */
function normalizeBranchCodes(raw: string[] | string | null | undefined): string[] {
  if (Array.isArray(raw)) return raw.map((c) => c.trim()).filter(Boolean)
  if (typeof raw === 'string') {
    return raw
      .split(',')
      .map((c) => c.trim())
      .filter(Boolean)
  }
  return []
}

export const useAuthStore = defineStore('auth', () => {
  // State — token in-memory tutulur, localStorage/sessionStorage'a yazılmaz
  const _accessToken = ref<string | null>(null)
  const user = ref<AuthUser | null>(null)
  const permissions = ref<string[]>([])
  const isInitialized = ref(false)

  // Getters
  const isAuthenticated = computed(() => !!_accessToken.value && !!user.value)
  const accessToken = computed(() => _accessToken.value)

  /**
   * Müdür veya Müdür Yardımcısı — alan seçicisini görebilir.
   * #129: müdür yardımcısı artık ayrı realm rolüdür (`DeputyDirector`); eklenmeseydi o role
   * sahip kullanıcı alan seçicisini kaybederdi. `InstitutionStaff` geriye dönük uyum için
   * listede kalır — henüz rolü güncellenmemiş müdür yardımcıları o rolde durabilir.
   */
  const isManager = computed(() =>
    user.value?.roles.some(
      (r) => r === 'InstitutionManager' || r === 'DeputyDirector' || r === 'InstitutionStaff',
    ) ?? false,
  )

  /** Alan Şefi veya yetkili koordinatör öğretmen — alan seçicisi göstermez, otomatik atanır */
  const isDepartmentHead = computed(() =>
    user.value?.roles.includes('DepartmentHead') ?? false,
  )

  function hasPermission(permission: string): boolean {
    return permissions.value.some(
      (p) =>
        p === permission ||
        // Wildcard: 'student:*' her 'student:' ile başlayan izni kapsar
        (p.endsWith(':*') && permission.startsWith(p.slice(0, -1))),
    )
  }

  function hasAnyPermission(perms: string[]): boolean {
    return perms.some((perm) => hasPermission(perm))
  }

  function hasAllPermissions(perms: string[]): boolean {
    return perms.every((perm) => hasPermission(perm))
  }

  /**
   * Kurum geneli alan muafiyeti (#126) — rol adına DEĞİL permission'a bakar.
   * Müdür ve müdür yardımcısı taşır; alan şefi taşımaz.
   */
  const canManageAllBranches = computed(() => hasPermission(ALL_BRANCHES_PERMISSION))

  /**
   * Kullanıcının **yazabileceği** alan kodları (#126).
   * Muafiyeti varsa `null` döner — "kısıt yok" demektir; boş dizi ile karıştırılmamalıdır
   * (boş dizi "hiçbir alana yazamaz" demektir).
   */
  const writableBranchCodes = computed<string[] | null>(() =>
    canManageAllBranches.value ? null : (user.value?.branchCodes ?? []),
  )

  /** Verilen alana yazma yetkisi var mı? Backend `BranchScopePolicy` ile aynı karar. */
  function canWriteBranch(branchCode: string | null | undefined): boolean {
    const scope = writableBranchCodes.value
    if (scope === null) return true
    if (!branchCode) return false
    return scope.some((c) => c.toLocaleLowerCase('tr') === branchCode.toLocaleLowerCase('tr'))
  }

  // Actions
  function setFromKeycloak(keycloak: Keycloak): void {
    const parsed = keycloak.tokenParsed as KeycloakTokenParsed | undefined

    if (!keycloak.token || !parsed) {
      clear()
      return
    }

    _accessToken.value = keycloak.token

    // Keycloak realm_access.roles → MESNET rolleri (InstitutionManager, Teacher vb.)
    const realmRoles = parsed.realm_access?.roles ?? []

    user.value = {
      id: parsed.sub,
      username: parsed.preferred_username,
      email: parsed.email,
      firstName: parsed.given_name ?? '',
      lastName: parsed.family_name ?? '',
      fullName:
        [parsed.given_name, parsed.family_name].filter(Boolean).join(' ') ||
        parsed.preferred_username,
      roles: realmRoles,
      institutionId: parsed.institution_id ?? null,
      branchCode: null,
      // Token'da claim varsa hemen kullan; yoksa loadPermissions() backend'den doldurur (#126)
      branchCodes: normalizeBranchCodes(parsed.branch_codes),
    }

    // Permission'lar henüz yüklenmedi — loadPermissions() ile backend'den alınacak
    isInitialized.value = true
  }

  // Backend'den güncel permission listesini ve kullanıcı bilgilerini yükle
  // PermissionClaimsTransformation roller → permission dönüşümünü yapar
  //
  // Aspire restart sonrası backend henüz ayağa kalkmamış olabilir.
  // Network hatası (abort, timeout, ECONNREFUSED) → retry ile bekle.
  // Gerçek 401 → token geçersiz → login redirect.
  async function loadPermissions(maxRetries = 5): Promise<LoadPermissionsOutcome> {
    if (!_accessToken.value) return 'reauth'

    // Gönderilecek token'ın ölüm anı — 401 sınıflandırmasının ayırt edici ölçütü (#136).
    const tokenExp = decodeTokenExp(_accessToken.value)

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        // axios interceptor ResponseBuilder.data'yı otomatik unwrap eder
        const { data } = await api.get('/auth/me')
        permissions.value = data?.permissions ?? []

        // Backend'den gelen bilgiler token claim'inden daha güvenilir
        if (user.value) {
          user.value = {
            ...user.value,
            ...(data?.institutionId ? { institutionId: data.institutionId } : {}),
            branchCode: data?.branchCode ?? null,
            branchCodes: normalizeBranchCodes(data?.branchCodes),
          }
        }
        return 'ok'
      } catch (err: unknown) {
        const axiosErr = err as { response?: { status?: number }; code?: string }
        const status = axiosErr.response?.status
        const code = axiosErr.code // 'ERR_NETWORK', 'ECONNABORTED', 'ERR_CANCELED'

        // Kararı saf sınıflandırıcı verir; yönlendirmeyi bu fonksiyon YAPMAZ (#136).
        //
        // Eskiden 401 koşulsuz "geçici" sayılırdı. Gerekçesi gerçekti — keycloak.init
        // sonrası 401 çoğunlukla API'nin JWKS önbelleğinin restart sonrası soğuk
        // olmasındandı ve hemen re-login sonsuz döngü üretirdi. Ama ÖLÜ token da aynı
        // kefeye giriyordu: 5 deneme × 1.5-6 sn = 15+ saniye beyaz ekran, sonunda login(),
        // sonra yeniden aynı yer. Ayrım artık token'ın yerel `exp`'i ile yapılır:
        // geçerli token + 401 = JWKS soğuk → tekrar dene; ölü token + 401 → yeniden giriş.
        const action = classifyAuthFailure({
          status, code, tokenExp, now: Date.now(), attempt, maxAttempts: maxRetries,
        })

        if (action === 'retry') {
          const delay = attempt * 1500 // 1.5s, 3s, 4.5s, 6s
          console.warn(`[Auth] /auth/me henüz hazır değil (durum: ${status ?? 'ağ'}), ${delay / 1000}s sonra tekrar... (${attempt}/${maxRetries})`)
          await new Promise((resolve) => setTimeout(resolve, delay))
          continue
        }

        if (action === 'reauth') {
          console.warn(`[Auth] Token reddedildi (durum: ${status ?? 'ağ'}) — yeniden giriş gerekiyor.`)
          return 'reauth'
        }

        console.error('[Auth] Permission yüklenemedi:', err)
        return 'give-up'
      }
    }

    return 'give-up'
  }

  // Token refresh sonrası sadece token güncelle (permission'lar backend'de 5dk cache'li)
  function refreshToken(newToken: string): void {
    _accessToken.value = newToken
  }

  function clear(): void {
    _accessToken.value = null
    user.value = null
    permissions.value = []
    isInitialized.value = false
  }

  return {
    user,
    permissions,
    isInitialized,
    isAuthenticated,
    accessToken,
    isManager,
    isDepartmentHead,
    canManageAllBranches,
    writableBranchCodes,
    canWriteBranch,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    setFromKeycloak,
    loadPermissions,
    refreshToken,
    clear,
  }
})
