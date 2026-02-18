import Keycloak from 'keycloak-js'
import { useAuthStore } from 'stores/auth'
import { useNotificationStore } from 'stores/notifications'

// Keycloak instance — singleton, uygulama boyunca tek
let _keycloak: Keycloak | null = null

export function getKeycloak(): Keycloak {
  if (!_keycloak) throw new Error('Keycloak henüz başlatılmadı')
  return _keycloak
}

export async function bootAuth(): Promise<void> {
  const keycloak = new Keycloak({
    url: import.meta.env.VITE_KEYCLOAK_URL as string,
    realm: import.meta.env.VITE_KEYCLOAK_REALM as string,
    clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID as string,
  })

  _keycloak = keycloak

  // PKCE S256 — Keycloak 26+ varsayılanı, açıkça belirt
  const authenticated = await keycloak.init({
    onLoad: 'login-required',   // login olmadan erişim yok
    pkceMethod: 'S256',
    checkLoginIframe: false,    // iframe polling devre dışı (CSP uyumu)
    silentCheckSsoFallback: false,
  })

  if (!authenticated) {
    // init() login-required ile dönüyorsa zaten redirect yapıyor
    // buraya normalde düşülmez
    return
  }

  const authStore = useAuthStore()
  authStore.setFromKeycloak(keycloak)

  // Backend'den güncel permission listesini al (roller → permission dönüşümü backend'de yapılır)
  await authStore.loadPermissions()

  // SSE bildirim bağlantısını aç
  const notificationStore = useNotificationStore()
  void notificationStore.connect()

  // 5 dakikada bir silent token refresh
  // onTokenExpired: token süresi dolduğunda
  keycloak.onTokenExpired = () => {
    keycloak
      .updateToken(60) // 60 sn kala yenile
      .then((refreshed) => {
        if (refreshed && keycloak.token) {
          authStore.refreshToken(keycloak.token)
        }
      })
      .catch(() => {
        // Refresh başarısızsa oturumu kapat
        authStore.clear()
        keycloak.logout()
      })
  }

  // Token refresh sonrası store güncelle
  keycloak.onAuthRefreshSuccess = () => {
    if (keycloak.token) {
      authStore.refreshToken(keycloak.token)
    }
  }

  // Başka sekmede logout olunursa
  keycloak.onAuthLogout = () => {
    authStore.clear()
  }
}

// Logout — Keycloak oturumunu da sonlandır
export async function logout(): Promise<void> {
  const authStore = useAuthStore()
  authStore.clear()

  const notificationStore = useNotificationStore()
  notificationStore.disconnect()

  if (_keycloak) {
    await _keycloak.logout({
      redirectUri: window.location.origin,
    })
  }
}
