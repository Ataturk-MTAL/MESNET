import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { useAuthStore } from 'stores/auth'
import { logger } from '../utils/logger'

export interface SseNotification {
  /**
   * İstemci tarafında üretilen kararlı kimlik — SSE payload'unda id yoktur.
   * v-for :key için gerekli: liste başa ekleniyor (unshift), bu yüzden dizi indeksi her
   * yeni bildirimde kayar ve indeks anahtarı aynı bildirimi "yeni öğe" gibi gösterir.
   */
  id: string
  eventType: string
  module: string
  payload: unknown
  occurredAt: string
  /** Okundu mu — store tarafında atanır (SSE payload'unda yoktur). */
  read?: boolean
}

/**
 * SSE Notification Store
 *
 * EventSource API header desteklemediğinden fetch() + ReadableStream kullanılır.
 * Axios interceptor'ındaki Bearer token, fetch çağrısına manuel eklenir.
 *
 * Bağlantı akışı:
 *   auth.ts boot → connect() → /api/notifications/stream (Bearer header ile)
 *   → heartbeat her 30s → disconnect() uygulama kapanırken
 */
export const useNotificationStore = defineStore('notifications', () => {
  const notifications = ref<SseNotification[]>([])
  const connected = ref(false)

  let abortController: AbortController | null = null

  /** Bildirim kimliği için artan sayaç — aynı ms'te gelen iki olay bile çakışmaz. */
  let notificationCounter = 0

  async function connect() {
    if (connected.value) return

    const authStore = useAuthStore()
    const token = authStore.accessToken
    if (!token) return

    abortController = new AbortController()

    try {
      const response = await fetch('/api/notifications/stream', {
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: 'text/event-stream',
        },
        signal: abortController.signal,
      })

      if (!response.ok || !response.body) {
        logger.warn(`[SSE] Bağlantı başarısız: ${response.status}`)
        // 401/403 — token geçersiz veya yetki yok, tekrar deneme
        return
      }

      connected.value = true

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        buffer += decoder.decode(value, { stream: true })

        // SSE mesajlarını çift newline ile ayır
        const parts = buffer.split('\n\n')
        buffer = parts.pop() ?? ''

        for (const part of parts) {
          const notification = parseSseBlock(part)
          if (notification) {
            // Sistem eventlerini (connection.established, keepalive) filtrele
            if (notification.eventType !== 'connection.established') {
              notifications.value.unshift({
                ...notification,
                id: `sse-${++notificationCounter}`,
                read: false,
              })
              // Max 50 bildirim tut
              if (notifications.value.length > 50) {
                notifications.value.pop()
              }
            }
          }
        }
      }
    } catch (err) {
      if ((err as Error).name !== 'AbortError') {
        logger.warn('[SSE] Stream hatası:', err)
      }
    } finally {
      connected.value = false
    }
  }

  function disconnect() {
    abortController?.abort()
    abortController = null
    connected.value = false
  }

  function clear() {
    notifications.value = []
  }

  /** Tek bir bildirimi listeden kaldırır (index-bazlı — SseNotification'da benzersiz id yok). */
  function remove(index: number) {
    notifications.value.splice(index, 1)
  }

  /** Tüm bildirimleri okundu işaretler (bildirim menüsü kapanınca çağrılır). */
  function markAllRead() {
    notifications.value.forEach((n) => {
      n.read = true
    })
  }

  /** Okunmamış bildirim sayısı (üst-bar rozeti). */
  const unreadCount = computed(() => notifications.value.filter((n) => !n.read).length)

  return { notifications, connected, connect, disconnect, clear, remove, markAllRead, unreadCount }
})

/**
 * SSE text bloğunu parse eder.
 * Format:
 *   id:uuid
 *   event:eventType
 *   data:{"eventType":"...", "module":"...", "payload":{...}, "occurredAt":"..."}
 */
function parseSseBlock(block: string): SseNotification | null {
  const lines = block.split('\n')
  let dataLine = ''

  for (const line of lines) {
    if (line.startsWith('data:')) {
      dataLine = line.slice(5).trim()
    }
  }

  if (!dataLine) return null

  try {
    return JSON.parse(dataLine) as SseNotification
  } catch {
    return null
  }
}
