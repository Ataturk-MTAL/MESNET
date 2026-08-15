import { Notify } from 'quasar'
import type { AxiosError } from 'axios'
import { logger } from '../utils/logger'

/**
 * Backend ApiResponse hata yapısından kullanıcıya gösterilecek mesajı çıkarır.
 * DomainException (422) → response.data.message (iş kuralı açıklaması)
 * Diğer hatalar → fallback mesajı
 */
export function extractApiError(err: unknown, fallback: string): string {
  const axiosErr = err as AxiosError<{ message?: string; code?: number | string }>
  const status = axiosErr?.response?.status
  const msg = axiosErr?.response?.data?.message
  // İş kuralı / doğrulama hataları (4xx) backend'in açıklayıcı mesajını taşır → kullanıcıya göster.
  // Sunucu/altyapı hataları (5xx), ağ/timeout → ham teknik metin gösterme, genel (fallback) mesaj ver.
  if (status && status >= 400 && status < 500 && typeof msg === 'string' && msg.length > 0) {
    return msg
  }
  return fallback
}

/** Geliştirici için tam teknik detayı tarayıcı konsoluna basar (kullanıcıya gösterilmez). */
function logApiError(err: unknown) {
  const axiosErr = err as AxiosError<{ message?: string; code?: number | string }>
  const res = axiosErr?.response
  logger.error('[API Hatası]', {
    status: res?.status,
    method: axiosErr?.config?.method?.toUpperCase(),
    url: axiosErr?.config?.url,
    code: res?.data?.code,
    serverMessage: res?.data?.message,
    error: err,
  })
}

export function useNotify() {
  function success(message: string) {
    Notify.create({
      type: 'positive',
      message,
      position: 'top-right',
      timeout: 3000,
    })
  }

  function error(message: string) {
    Notify.create({
      type: 'negative',
      message,
      position: 'top-right',
      timeout: 5000,
    })
  }

  /**
   * Backend API hatasını işler: geliştirici detayını KONSOLA basar (kullanıcıya gösterilmez),
   * kullanıcıya temiz/anlaşılır bir mesaj gösterir.
   */
  function apiError(err: unknown, fallback: string) {
    logApiError(err)
    error(extractApiError(err, fallback))
  }

  function warning(message: string) {
    Notify.create({
      type: 'warning',
      message,
      position: 'top-right',
      timeout: 4000,
    })
  }

  function info(message: string) {
    Notify.create({
      type: 'info',
      message,
      position: 'top-right',
      timeout: 3000,
    })
  }

  return { success, error, apiError, warning, info }
}
