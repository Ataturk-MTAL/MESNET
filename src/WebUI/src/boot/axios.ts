import axios, { AxiosError } from 'axios'
import { useAuthStore } from 'stores/auth'
import { decodeTokenExp, isTokenExpired } from 'src/utils/authFailure'
import { getKeycloak } from './auth'

const api = axios.create({
  baseURL: '/api',
  timeout: 30_000,
  headers: { 'Content-Type': 'application/json' },
})

// Her istekte güncel access token'ı Authorization header'a ekle
// Token expire olmuş veya 30 sn içinde expire olacaksa proaktif yenile
// FormData gönderiminde Content-Type header'ını sil (Axios boundary ile otomatik set eder)
api.interceptors.request.use(async (config) => {
  const authStore = useAuthStore()

  try {
    const keycloak = getKeycloak()
    // 30 sn'den az ömrü kaldıysa yenile — 401 döngüsünü önler
    const refreshed = await keycloak.updateToken(30)
    if (refreshed && keycloak.token) {
      authStore.refreshToken(keycloak.token)
    }
  } catch {
    // Yenileme başarısız. Eskiden burada mevcut token ile DEVAM ediliyordu; ölü token
    // API'ye gidip 401 alıyor, 401 "geçici" sayılıp tekrar deneniyor ve döngü kuruluyordu (#136).
  }

  const token = authStore.accessToken

  // Öldüğünü ZATEN bildiğimiz token gönderilmez (#136). Karar ağa çıkmadan, yerel `exp`
  // ile verilir: sunucuya sormak için sunucuya ölü token göndermek gerekirdi.
  if (token && isTokenExpired(decodeTokenExp(token), Date.now())) {
    const { reauthenticate } = await import('./auth')
    reauthenticate('istek anında token süresi dolmuş')

    // İstek hiç yapılmaz. Yönlendirme başladı; bu hata yalnız bekleyen çağrıyı çözer.
    return Promise.reject(new AxiosError('Oturum süresi doldu', 'AUTH_EXPIRED', config))
  }

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  if (config.data instanceof FormData) {
    delete config.headers['Content-Type']
  }

  return config
})

// ResponseBuilder wrapper'ını unwrap et: { code, data, ... } → data kısmını çıkar
// Böylece component'lerde res.data ile doğrudan veriye erişilir
api.interceptors.response.use(
  (response) => {
    const body = response.data
    if (body && typeof body === 'object' && 'code' in body && 'data' in body) {
      response.data = body.data
    }
    return response
  },
  async (error) => {
    const originalRequest = error.config as typeof error.config & {
      _retry?: boolean
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      try {
        const keycloak = getKeycloak()

        // -1 = zorla yenile. Eskiden 0 geçiliyordu ve yorumu "zorla yenile" diyordu, ama
        // keycloak-js `minValidity = minValidity || 5` yapar (keycloak.js:1461): 0 sessizce
        // 5'e dönüşür ve yerel olarak geçerli token'da yenileme HİÇ yapılmadan false döner.
        // Yani bu dal, tam da yazıldığı durumda (API'nin JWKS önbelleği soğuk, token geçerli)
        // hiç çalışmıyordu (#136).
        const refreshed = await keycloak.updateToken(-1)

        if (refreshed && keycloak.token) {
          const authStore = useAuthStore()
          authStore.refreshToken(keycloak.token)
          originalRequest.headers.Authorization = `Bearer ${keycloak.token}`
          return api(originalRequest)
        }
      } catch {
        // Yenileme de başarısız → tek yeniden giriş hunisi (#136). Eskiden burada doğrudan
        // logout() çağrılıyor ve aynı anda tetiklenen login() ile yarışıyordu; hangisi
        // window.location'a son yazarsa o kazanıyordu. login() kazandığında kapatılması
        // gereken Keycloak oturumu hayatta kalıyor ve döngü yeniden kuruluyordu.
        //
        // Boot sırasındaki koruma korunuyor: uygulama henüz ayağa kalkmadıysa bootAuth
        // zaten kendi yolundan yeniden giriş yapar; buradan ikinci bir yönlendirme
        // tetiklemek o akışı ezerdi.
        const authStore = useAuthStore()
        if (authStore.isInitialized) {
          const { reauthenticate } = await import('./auth')
          reauthenticate('401 sonrası token yenilenemedi')
        }
      }
    }

    return Promise.reject(error)
  },
)

export default api
