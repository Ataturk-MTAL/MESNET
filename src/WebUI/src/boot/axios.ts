import axios from 'axios'
import { useAuthStore } from 'stores/auth'
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
    // Refresh başarısızsa mevcut token ile devam et — 401 interceptor halleder
  }

  const token = authStore.accessToken
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
        const refreshed = await keycloak.updateToken(0) // zorla yenile

        if (refreshed && keycloak.token) {
          const authStore = useAuthStore()
          authStore.refreshToken(keycloak.token)
          originalRequest.headers.Authorization = `Bearer ${keycloak.token}`
          return api(originalRequest)
        }
      } catch {
        // Refresh da başarısız — logout
        const { logout } = await import('./auth')
        await logout()
      }
    }

    return Promise.reject(error)
  },
)

export default api
