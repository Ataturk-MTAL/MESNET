import { Notify } from 'quasar'
import type { AxiosError } from 'axios'

/**
 * Backend ApiResponse hata yapısından kullanıcıya gösterilecek mesajı çıkarır.
 * DomainException (422) → response.data.message (iş kuralı açıklaması)
 * Diğer hatalar → fallback mesajı
 */
export function extractApiError(err: unknown, fallback: string): string {
  const axiosErr = err as AxiosError<{ message?: string; code?: number }>
  const msg = axiosErr?.response?.data?.message
  if (msg && typeof msg === 'string') return msg
  return fallback
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

  /** Backend API hatasından mesaj çıkarır ve bildirim gösterir. */
  function apiError(err: unknown, fallback: string) {
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
