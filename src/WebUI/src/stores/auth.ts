import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type Keycloak from 'keycloak-js'
import api from 'boot/axios'

// Keycloak token payload
interface KeycloakTokenParsed {
  sub: string
  preferred_username: string
  email: string
  given_name?: string
  family_name?: string
  realm_access?: { roles: string[] }
  institution_id?: string
}

export interface AuthUser {
  id: string
  username: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  roles: string[]
  institutionId: string | null
  branchCode: string | null
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

  /** Müdür veya Müdür Yardımcısı — alan seçicisini görebilir */
  const isManager = computed(() =>
    user.value?.roles.some((r) => r === 'InstitutionManager' || r === 'InstitutionStaff') ?? false,
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
  async function loadPermissions(maxRetries = 5): Promise<void> {
    if (!_accessToken.value) return

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
          }
        }
        return // başarılı — çık
      } catch (err: unknown) {
        const axiosErr = err as { response?: { status?: number }; code?: string }
        const status = axiosErr.response?.status
        const code = axiosErr.code // 'ERR_NETWORK', 'ECONNABORTED', 'ERR_CANCELED'

        // 401 veya ağ hatası → GEÇİCİ kabul et + retry. keycloak.init (login-required) hemen
        // sonrası token KESİN geçerlidir; bu noktada 401 büyük olasılıkla API'nin JWKS'i restart
        // sonrası henüz yüklemediğindendir. HEMEN re-login YAPMA → aynı geçerli token → 401 →
        // re-login → sonsuz refresh döngüsü. Önce retry, ısrar ederse re-login.
        const isTransient = !status || status === 401
          || code === 'ERR_NETWORK' || code === 'ECONNABORTED' || code === 'ERR_CANCELED'
        if (isTransient && attempt < maxRetries) {
          const delay = attempt * 1500 // 1.5s, 3s, 4.5s, 6s
          console.warn(`[Auth] /auth/me henüz hazır değil (durum: ${status ?? 'ağ'}), ${delay / 1000}s sonra tekrar... (${attempt}/${maxRetries})`)
          await new Promise((resolve) => setTimeout(resolve, delay))
          continue
        }

        // Tüm denemeler tükendi ve hâlâ 401 → token gerçekten reddedildi → yeniden giriş
        if (status === 401) {
          console.warn('[Auth] Token ısrarla reddedildi, yeniden giriş yapılıyor...')
          const { getKeycloak } = await import('boot/auth')
          await getKeycloak().login()
          return
        }

        // Ağ/diğer hata — sessiz kal, uygulama permission'sız başlasın
        console.error('[Auth] Permission yüklenemedi:', err)
      }
    }
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
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    setFromKeycloak,
    loadPermissions,
    refreshToken,
    clear,
  }
})
