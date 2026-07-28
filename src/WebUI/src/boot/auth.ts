import Keycloak from 'keycloak-js'
import { useAuthStore } from 'stores/auth'
import { useNotificationStore } from 'stores/notifications'
import { decideReauth, recordReauth } from 'src/utils/authFailure'
import { showSessionExpiredScreen } from './sessionExpiredScreen'

// Keycloak instance — singleton, uygulama boyunca tek
let _keycloak: Keycloak | null = null

export function getKeycloak(): Keycloak {
  if (!_keycloak) throw new Error('Keycloak henüz başlatılmadı')
  return _keycloak
}

/**
 * Yeniden giriş denemelerinin zaman damgaları (#136).
 *
 * <p><b>sessionStorage şart:</b> #136'daki döngü sayfa yüklemeleri ARASINDA dönüyordu.
 * Her yönlendirme JS bağlamını yok ettiği için bellekteki her sayaç sıfırlanır ve döngüyü
 * asla göremez. sessionStorage sekmeye özeldir, yönlendirmeyi aşar, sekme kapanınca gider —
 * tam da istenen kapsam.</p>
 */
const REAUTH_LOG_KEY = 'mesnet.reauth.attempts'

function readReauthLog(): number[] {
  try {
    const raw = sessionStorage.getItem(REAUTH_LOG_KEY)
    if (!raw) return []
    const parsed: unknown = JSON.parse(raw)
    return Array.isArray(parsed) ? parsed.filter((t): t is number => typeof t === 'number') : []
  } catch {
    // sessionStorage erişilemiyor olabilir (gizli sekme kotası, bozuk değer).
    // Döngü kırıcısını kaybetmek kötü ama uygulamayı burada çökertmek daha kötü.
    return []
  }
}

function writeReauthLog(timestamps: number[]): void {
  try {
    sessionStorage.setItem(REAUTH_LOG_KEY, JSON.stringify(timestamps))
  } catch {
    // bkz. readReauthLog
  }
}

/** Başarılı boot sonrası çağrılır — sağlıklı bir oturum sayacı sıfırlar. */
function clearReauthLog(): void {
  try {
    sessionStorage.removeItem(REAUTH_LOG_KEY)
  } catch {
    // bkz. readReauthLog
  }
}

/**
 * <b>Tek yeniden giriş hunisi</b> (#136).
 *
 * <p>Önceden dört ayrı kaçış yolu vardı — <c>boot/auth.ts</c> boot yenileme catch'i,
 * <c>stores/auth.ts</c> <c>loadPermissions</c>, <c>boot/axios.ts</c> yanıt interceptor'ı ve
 * <c>onTokenExpired</c> — ve bunlardan üçü <c>login()</c>, biri <c>logout()</c> çağırıyordu.
 * Aynı anda tetiklenip birbirinin yönlendirmesini eziyorlardı; <c>login()</c> kazandığında
 * kapatılması gereken oturum hayatta kalıyor ve döngü yeniden kuruluyordu.</p>
 *
 * <p>Limit aşılırsa yönlendirme YAPILMAZ: kullanıcıya Türkçe ekran gösterilir ve oradaki
 * düğme <b>çıkış</b> yapar — döngüyü besleyen Keycloak oturumunu yok eden tek işlem odur.</p>
 */
export function reauthenticate(reason: string): void {
  const now = Date.now()
  const log = readReauthLog()

  if (decideReauth(log, now) === 'halt') {
    console.error(`[Auth] Yeniden giriş döngüsü kırıldı (${reason}) — oturum ekranı gösteriliyor.`)
    clearReauthLog()
    showSessionExpiredScreen({
      detail: 'Yeniden giriş birkaç kez denendi ancak oturum doğrulanamadı.',
      onLogout: () => {
        logout().catch(() => {})
      },
    })
    return
  }

  writeReauthLog(recordReauth(log, now))
  console.warn(`[Auth] Yeniden giriş yapılıyor (${reason})...`)

  // login() tam sayfa yönlendirmedir; döndüğü promise ASLA settle olmaz.
  // await edilirse çağıran sonsuza kadar askıda kalır — #136'daki beyaz ekranın
  // sebeplerinden biri buydu. Bilerek await edilmiyor.
  getKeycloak().login().catch(() => {})
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
    redirectUri: window.location.origin + window.location.pathname,
  })

  if (!authenticated) {
    // init() login-required ile dönüyorsa zaten redirect yapıyor
    // buraya normalde düşülmez
    return
  }

  const authStore = useAuthStore()

  // Token henüz expire olmadığından emin ol — Keycloak session recovery sonrası
  // eski token gelmiş olabilir, zorla yenile
  try {
    const refreshed = await keycloak.updateToken(60) // 60 sn'den az kaldıysa yenile
    if (refreshed) {
      console.info('[Auth] Token boot sırasında yenilendi')
    }
  } catch {
    // Refresh başarısız — eski session geçersiz, yeniden giriş gerekli
    reauthenticate('boot token yenilemesi başarısız')
    return
  }

  authStore.setFromKeycloak(keycloak)

  // Backend'den güncel permission listesini al
  // Aspire restart sonrası backend henüz hazır olmayabilir — retry ile bekle.
  // Karar loadPermissions'ta VERİLMEZ, sonucu döndürür; yönlendirme tek huniden geçer (#136).
  const outcome = await authStore.loadPermissions()

  if (outcome === 'reauth') {
    reauthenticate('/auth/me isteği token reddiyle döndü')
    return
  }

  if (outcome === 'give-up') {
    // Yeniden giriş bunu çözmez (backend yok / ağ kopuk). Yönlendirme yerine ekran:
    // sessiz beyaz ekran yerine kullanıcı ne olduğunu görsün.
    showSessionExpiredScreen({
      detail: 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edip yeniden deneyin.',
      onLogout: () => {
        logout().catch(() => {})
      },
    })
    return
  }

  // Buraya gelindiyse oturum sağlıklı — döngü sayacı sıfırlanır ki ileride tek bir
  // gerçek hata, geçmiş denemelerin üstüne binip erkenden limiti tetiklemesin.
  clearReauthLog()

  // SSE bildirim bağlantısını aç (permission yüklendiyse)
  if (authStore.permissions.length > 0) {
    const notificationStore = useNotificationStore()
    notificationStore.connect().catch(() => {})
  }

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
        // Yenileme başarısız — tek huniden geç (#136). Eskiden burası doğrudan
        // logout() çağırıyordu ve aynı anda tetiklenen login() ile yarışıyordu;
        // ayrıca clear() isInitialized'ı düşürüp axios'taki logout kapısını kapatıyordu.
        reauthenticate('onTokenExpired yenilemesi başarısız')
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
