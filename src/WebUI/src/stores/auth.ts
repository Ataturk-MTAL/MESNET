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
    }

    // Permission'lar henüz yüklenmedi — loadPermissions() ile backend'den alınacak
    isInitialized.value = true
  }

  // Backend'den güncel permission listesini yükle
  // PermissionClaimsTransformation roller → permission dönüşümünü yapar
  async function loadPermissions(): Promise<void> {
    if (!_accessToken.value) return

    try {
      // axios interceptor ResponseBuilder.data'yı otomatik unwrap eder
      const { data } = await api.get('/auth/me')
      permissions.value = data?.permissions ?? []
    } catch (err) {
      console.error('Permission yüklenirken hata:', err)
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
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    setFromKeycloak,
    loadPermissions,
    refreshToken,
    clear,
  }
})
